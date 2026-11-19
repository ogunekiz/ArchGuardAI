using ArchGuard.Domain.Enums;

namespace ArchGuard.Application.DTOs;

public class RunAuditRequestDto
{
	public Guid ProjectId { get; set; }
	public LlmProviderType Provider { get; set; } = LlmProviderType.Gemini;
}

public class AuditFindingResponseDto
{
	public Guid Id { get; set; }
	public string ElementGlobalId { get; set; } = string.Empty;
	public string ElementType { get; set; } = string.Empty;
	public string ElementName { get; set; } = string.Empty;
	public string StoreyName { get; set; } = string.Empty;
	public string Severity { get; set; } = string.Empty;
	public string RuleReference { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string SuggestedFix { get; set; } = string.Empty;
	public string LlmProviderUsed { get; set; } = string.Empty;
	public DateTime AuditedAtUtc { get; set; }
}

public class BimChatRequestDto
{
	public Guid ProjectId { get; set; }
	public string Message { get; set; } = string.Empty;
	public LlmProviderType Provider { get; set; } = LlmProviderType.Gemini;
}

public class BimChatResponseDto
{
	public string Reply { get; set; } = string.Empty;
	public string ProviderUsed { get; set; } = string.Empty;
}