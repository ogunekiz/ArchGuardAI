namespace ArchGuard.Application.DTOs;

public class RegulationRuleDto
{
	public string RuleCode { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
}

public class RegulationSearchResultDto
{
	public string RuleCode { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public float Score { get; set; }
}