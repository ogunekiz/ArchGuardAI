using ArchGuard.Domain.Enums;

namespace ArchGuard.Domain.Entities;

public class AuditFinding
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid BimProjectId { get; set; }
	public string ElementGlobalId { get; set; } = string.Empty;
	public string ElementType { get; set; } = string.Empty; // Door, Space, Wall
	public string ElementName { get; set; } = string.Empty;
	public string StoreyName { get; set; } = string.Empty;
	public AuditSeverity Severity { get; set; }
	public string RuleReference { get; set; } = string.Empty; // Örn: "Yangın Yönetmeliği Madde 47"
	public string Description { get; set; } = string.Empty;
	public string SuggestedFix { get; set; } = string.Empty;
	public string LlmProviderUsed { get; set; } = string.Empty;
	public DateTime AuditedAtUtc { get; set; } = DateTime.UtcNow;

	public BimProject BimProject { get; set; } = null!;
}