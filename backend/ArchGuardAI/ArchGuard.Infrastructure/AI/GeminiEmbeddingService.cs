using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ArchGuard.Infrastructure.AI;

public class GeminiEmbeddingService
{
	private readonly HttpClient _httpClient;
	private readonly string _apiKey;
	private readonly string _baseEndpoint;

	public GeminiEmbeddingService(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_apiKey = configuration["AiProviders:Gemini:ApiKey"] ?? string.Empty;
		_baseEndpoint = (configuration["AiProviders:Gemini:Endpoint"]
				?? "https://generativelanguage.googleapis.com/v1beta/openai/").TrimEnd('/');
	}

	public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.StartsWith("BURAYA_"))
			throw new InvalidOperationException("Lütfen appsettings.Development.json içine geçerli bir Gemini ApiKey girin.");

		var url = $"{_baseEndpoint}/embeddings";

		var payload = new
		{
			model = "gemini-embedding-001",
			input = text,
			dimensions = 768
		};

		using var request = new HttpRequestMessage(HttpMethod.Post, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
		request.Content = JsonContent.Create(payload);

		var response = await _httpClient.SendAsync(request, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var err = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new InvalidOperationException($"Gemini Embedding hatası ({response.StatusCode}): {err}");
		}

		using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
		var values = doc.RootElement
				.GetProperty("data")[0]
				.GetProperty("embedding")
				.EnumerateArray()
				.Select(x => x.GetSingle())
				.ToArray();

		return values;
	}
}