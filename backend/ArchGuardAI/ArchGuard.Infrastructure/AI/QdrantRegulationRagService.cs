using ArchGuard.Application.DTOs;
using ArchGuard.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using UglyToad.PdfPig;

namespace ArchGuard.Infrastructure.AI;

public class QdrantRegulationRagService : IRegulationRagService
{
	private const string CollectionName = "turkish_building_regulations";
	private const ulong VectorSize = 768; // Gemini text-embedding-004 boyutu

	private readonly QdrantClient _qdrantClient;
	private readonly GeminiEmbeddingService _embeddingService;

	public QdrantRegulationRagService(IConfiguration configuration, GeminiEmbeddingService embeddingService)
	{
		var host = configuration["ExternalServices:QdrantHost"] ?? "localhost";
		var grpcPort = int.Parse(configuration["ExternalServices:QdrantGrpcPort"] ?? "6354");

		_qdrantClient = new QdrantClient(host, grpcPort);
		_embeddingService = embeddingService;
	}

	private async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken)
	{
		var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);
		if (!collections.Contains(CollectionName))
		{
			await _qdrantClient.CreateCollectionAsync(
					CollectionName,
					new VectorParams { Size = VectorSize, Distance = Distance.Cosine },
					cancellationToken: cancellationToken);
		}
	}

	public async Task IngestPdfAsync(Stream pdfStream, string category, CancellationToken cancellationToken = default)
	{
		await EnsureCollectionExistsAsync(cancellationToken);

		// Sadece mimari, kapı, mahal, yangın ve metraj içeren ilgili sayfaları süzer (kota ve hız dostu)
		string[] architecturalKeywords = ["kapı", "kaçış", "mahal", "piyes", "genişlik", "yükseklik", "yangın", "merdiven", "metrekare", "m2", "erişilebilirlik"];

		var points = new List<PointStruct>();
		using var document = PdfDocument.Open(pdfStream);
		int processedPages = 0;

		foreach (var page in document.GetPages())
		{
			if (processedPages >= 15) break; // Tek seferde en fazla 15 ilgili sayfa indeksle (Gemini geçici kota koruması)

			var text = page.Text;
			if (string.IsNullOrWhiteSpace(text)) continue;

			var lowerText = text.ToLowerInvariant();
			bool isRelevant = architecturalKeywords.Any(kw => lowerText.Contains(kw));
			if (!isRelevant) continue;

			var chunks = ChunkText(text, 900, 120);
			int chunkIndex = 1;

			foreach (var chunk in chunks)
			{
				var vector = await _embeddingService.GenerateEmbeddingAsync(chunk, cancellationToken);
				var point = new PointStruct
				{
					Id = Guid.NewGuid(),
					Vectors = vector,
					Payload =
										{
												["rule_code"] = $"{category} - Sayfa {page.Number} (Bölüm {chunkIndex})",
												["category"] = category,
												["content"] = chunk
										}
				};
				points.Add(point);
				chunkIndex++;
			}

			processedPages++;
			await Task.Delay(250, cancellationToken); // API hız sınırına takılmamak için kısa bekleme
		}

		if (points.Count > 0)
		{
			await _qdrantClient.UpsertAsync(CollectionName, points, cancellationToken: cancellationToken);
		}
	}

	public async Task<List<RegulationSearchResultDto>> SearchRelevantRulesAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
	{
		await EnsureCollectionExistsAsync(cancellationToken);

		var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
		var searchResults = await _qdrantClient.SearchAsync(
				CollectionName,
				queryVector,
				limit: (ulong)topK,
				cancellationToken: cancellationToken);

		return searchResults.Select(r => new RegulationSearchResultDto
		{
			RuleCode = r.Payload.TryGetValue("rule_code", out var rc) ? rc.StringValue : string.Empty,
			Category = r.Payload.TryGetValue("category", out var cat) ? cat.StringValue : string.Empty,
			Content = r.Payload.TryGetValue("content", out var cnt) ? cnt.StringValue : string.Empty,
			Score = r.Score
		}).ToList();
	}

	private static List<string> ChunkText(string text, int chunkSize, int overlap)
	{
		var chunks = new List<string>();
		int start = 0;
		while (start < text.Length)
		{
			int length = Math.Min(chunkSize, text.Length - start);
			chunks.Add(text.Substring(start, length).Trim());
			start += (chunkSize - overlap);
		}
		return chunks;
	}
}