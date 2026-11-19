namespace ArchGuard.Application.DTOs;

public class ProjectSummaryResponseDto
{
	public Guid ProjectId { get; set; }
	public string ProjectName { get; set; } = string.Empty;
	public string SourceFileName { get; set; } = string.Empty;
	public string SourceType { get; set; } = string.Empty;
	public DateTime CreatedAtUtc { get; set; }
	public int StoreyCount { get; set; }
	public int SpaceCount { get; set; }
	public int DoorCount { get; set; }
	public int WallCount { get; set; }
	public double TotalWallAreaM2 { get; set; }
	public double TotalWallVolumeM3 { get; set; }
}