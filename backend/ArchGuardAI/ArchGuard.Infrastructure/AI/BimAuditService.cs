using System.Text.Json;
using System.Text.Json.Serialization;
using ArchGuard.Application.DTOs;
using ArchGuard.Application.Interfaces;
using ArchGuard.Domain.Entities;
using ArchGuard.Domain.Enums;
using ArchGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ArchGuard.Infrastructure.AI;

public class BimAuditService : IBimAuditService
{
	private readonly AppDbContext _dbContext;
	private readonly IDynamicKernelFactory _kernelFactory;
	private readonly IRegulationRagService _ragService;

	public BimAuditService(
			AppDbContext dbContext,
			IDynamicKernelFactory kernelFactory,
			IRegulationRagService ragService)
	{
		_dbContext = dbContext;
		_kernelFactory = kernelFactory;
		_ragService = ragService;
	}

	public async Task<List<AuditFindingResponseDto>> RunComplianceAuditAsync(
			Guid projectId,
			LlmProviderType provider,
			CancellationToken cancellationToken = default)
	{
		var project = await _dbContext.BimProjects
				.Include(p => p.Doors)
				.Include(p => p.Spaces)
				.Include(p => p.Walls)
				.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
				?? throw new KeyNotFoundException("Belirtilen BIM projesi bulunamadı.");

		// 1. Qdrant'tan yönetmelik ve BIM standart kurallarını çek (RAG Retrieval)
		var doorRules = await _ragService.SearchRelevantRulesAsync("kapı genişliği yangın çıkışı erişilebilirlik yangın dayanımı", 3, cancellationToken);
		var spaceAndWallRules = await _ragService.SearchRelevantRulesAsync("oda mahal net alan metrekare duvar metraj veri bütünlüğü", 3, cancellationToken);

		var allRules = doorRules
				.Concat(spaceAndWallRules)
				.GroupBy(r => r.RuleCode)
				.Select(g => g.First())
				.ToList();

		var rulesContext = string.Join("\n", allRules.Select(r => $"- [{r.RuleCode}]: {r.Content}"));

		// 2. Model elemanlarını kompakt JSON olarak hazırla (token tasarrufu için ilk 40 eleman özeti)
		var bimSnapshot = JsonSerializer.Serialize(new
		{
			Project = project.ProjectName,
			Doors = project.Doors.Take(40).Select(d => new
			{
				d.GlobalId,
				d.Name,
				d.StoreyName,
				d.WidthMeters,
				d.HeightMeters,
				d.IsFireExit,
				d.FireRating
			}),
			Spaces = project.Spaces.Take(40).Select(s => new
			{
				s.GlobalId,
				s.Name,
				s.StoreyName,
				s.AreaM2
			}),
			WallsSample = project.Walls.Take(20).Select(w => new
			{
				w.GlobalId,
				w.Name,
				w.StoreyName,
				w.IsExternal,
				w.AreaM2,
				w.VolumeM3
			})
		});

		// 3. Seçilen LLM Sağlayıcı (Gemini veya NVIDIA) ile Kernel oluştur
		var kernel = _kernelFactory.CreateKernel(provider, includeBimTools: false);
		var chatService = kernel.GetRequiredService<IChatCompletionService>();

		var systemPrompt = """
            Sen kıdemli bir BIM ve Mimari Yönetmelik Denetim Mühendisisin.
            Görevin: Verilen yönetmelik kurallarını (RAG bağlamı) kullanarak BIM modelindeki elemanları (kapılar, mahaller, duvarlar) tek tek denetlemek ve SADECE geçerli bir JSON dizisi döndürmektir.
            Markdown kod bloğu (```json) KULLANMA, açıklama metni YAZMA, doğrudan JSON objesini dön.

            Döndüreceğin JSON formatı tam olarak şu şemaya uymalıdır:
            {
              "findings": [
                {
                  "elementGlobalId": "elemanın GlobalId değeri",
                  "elementType": "Door | Space | Wall",
                  "elementName": "elemanın adı",
                  "storeyName": "bulunduğu kat",
                  "severity": "Error | Warning | Passed",
                  "ruleReference": "ilgili yönetmelik madde adı",
                  "description": "tespit edilen durumun Türkçe teknik açıklaması",
                  "suggestedFix": "mimarın Revit/Rhino modelinde yapması gereken düzeltme önerisi"
                }
              ]
            }

            Kurallar:
            - Kapı genişliği 0.90m altındaysa veya FireRating 'Belirtilmemiş' ise mutlaka Error veya Warning olarak raporla.
            - Mahal alanı (AreaM2) 0 ise veya imar yönetmeliği sınırının altındaysa raporla.
            - En az 5, en fazla 15 anlamlı bulgu üret (hem hataları hem de uygun olan örnekleri göster).
            """;

		var userPrompt = $"""
            === YÖNETMELİK VE STANDART KURALLARI ===
            {rulesContext}

            === DENETLENECEK BIM MODEL VERİSİ ===
            {bimSnapshot}
            """;

		var history = new ChatHistory();
		history.AddSystemMessage(systemPrompt);
		history.AddUserMessage(userPrompt);

		var settings = new OpenAIPromptExecutionSettings
		{
			Temperature = 0.1
		};

		if (provider == LlmProviderType.Gemini)
		{
			settings.ResponseFormat = "json_object";
		}

		var response = await chatService.GetChatMessageContentAsync(history, settings, kernel, cancellationToken);
		var rawJson = CleanJsonOutput(response.Content ?? "{}");

		var parsedResult = JsonSerializer.Deserialize<StructuredAuditWrapper>(rawJson, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		}) ?? new StructuredAuditWrapper();

		// 4. Önceki denetim bulgularını temizle ve yeni bulguları PostgreSQL'e kaydet
		var oldFindings = await _dbContext.AuditFindings
				.Where(f => f.BimProjectId == projectId)
				.ToListAsync(cancellationToken);

		if (oldFindings.Count > 0)
			_dbContext.AuditFindings.RemoveRange(oldFindings);

		var providerName = provider == LlmProviderType.Gemini ? "Google Gemini" : "NVIDIA NIM";
		var newEntities = parsedResult.Findings.Select(f => new AuditFinding
		{
			BimProjectId = projectId,
			ElementGlobalId = f.ElementGlobalId,
			ElementType = f.ElementType,
			ElementName = f.ElementName,
			StoreyName = f.StoreyName,
			Severity = ParseSeverity(f.Severity),
			RuleReference = f.RuleReference,
			Description = f.Description,
			SuggestedFix = f.SuggestedFix,
			LlmProviderUsed = providerName,
			AuditedAtUtc = DateTime.UtcNow
		}).ToList();

		_dbContext.AuditFindings.AddRange(newEntities);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return newEntities.Select(MapToDto).ToList();
	}

	public async Task<List<AuditFindingResponseDto>> GetProjectFindingsAsync(Guid projectId, CancellationToken cancellationToken = default)
	{
		var findings = await _dbContext.AuditFindings
				.Where(f => f.BimProjectId == projectId)
				.OrderByDescending(f => f.Severity)
				.ThenBy(f => f.StoreyName)
				.ToListAsync(cancellationToken);

		return findings.Select(MapToDto).ToList();
	}

	public async Task<BimChatResponseDto> ChatWithBimModelAsync(
			Guid projectId,
			string userMessage,
			LlmProviderType provider,
			CancellationToken cancellationToken = default)
	{
		// Tool Calling aktif şekilde Kernel üret
		var kernel = _kernelFactory.CreateKernel(provider, includeBimTools: true, projectId: projectId);
		var chatService = kernel.GetRequiredService<IChatCompletionService>();

		var history = new ChatHistory();
		history.AddSystemMessage("""
            Sen mimarlık ofisi için geliştirilmiş akıllı bir BIM & Metraj Asistanısın.
            Kullanıcının bina modeliyle ilgili sorularını yanıtlarken ASLA sayı uydurma.
            Her zaman sana tanımlı 'BimModelTools' fonksiyonlarını çağırarak gerçek veritabanı değerlerini oku ve Türkçe, net, mühendislik dilinde yanıt ver.
            """);
		history.AddUserMessage(userMessage);

		var settings = new OpenAIPromptExecutionSettings
		{
			Temperature = 0.2,
			FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
		};

		var result = await chatService.GetChatMessageContentAsync(history, settings, kernel, cancellationToken);
		var providerLabel = provider == LlmProviderType.Gemini ? "Google Gemini" : "NVIDIA NIM";

		return new BimChatResponseDto
		{
			Reply = result.Content ?? "Yanıt üretilemedi.",
			ProviderUsed = providerLabel
		};
	}

	private static AuditSeverity ParseSeverity(string raw) => raw.Trim().ToLowerInvariant() switch
	{
		"error" => AuditSeverity.Error,
		"warning" => AuditSeverity.Warning,
		_ => AuditSeverity.Passed
	};

	private static string CleanJsonOutput(string content)
	{
		var trimmed = content.Trim();
		if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
			trimmed = trimmed[7..];
		else if (trimmed.StartsWith("```"))
			trimmed = trimmed[3..];

		if (trimmed.EndsWith("```"))
			trimmed = trimmed[..^3];

		return trimmed.Trim();
	}

	private static AuditFindingResponseDto MapToDto(AuditFinding f) => new()
	{
		Id = f.Id,
		ElementGlobalId = f.ElementGlobalId,
		ElementType = f.ElementType,
		ElementName = f.ElementName,
		StoreyName = f.StoreyName,
		Severity = f.Severity.ToString(),
		RuleReference = f.RuleReference,
		Description = f.Description,
		SuggestedFix = f.SuggestedFix,
		LlmProviderUsed = f.LlmProviderUsed,
		AuditedAtUtc = f.AuditedAtUtc
	};

	private sealed class StructuredAuditWrapper
	{
		[JsonPropertyName("findings")]
		public List<StructuredAuditItem> Findings { get; set; } = new();
	}

	private sealed class StructuredAuditItem
	{
		public string ElementGlobalId { get; set; } = string.Empty;
		public string ElementType { get; set; } = string.Empty;
		public string ElementName { get; set; } = string.Empty;
		public string StoreyName { get; set; } = string.Empty;
		public string Severity { get; set; } = "Warning";
		public string RuleReference { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string SuggestedFix { get; set; } = string.Empty;
	}
}