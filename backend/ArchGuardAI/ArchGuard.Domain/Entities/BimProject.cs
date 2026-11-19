namespace ArchGuard.Domain.Entities;

public class BimProject
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string ProjectName { get; set; } = string.Empty;
	public string SourceFileName { get; set; } = string.Empty;
	public string SourceType { get; set; } = "IFC"; // "IFC" veya "RevitPlugin"
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

	public ICollection<BimStorey> Storeys { get; set; } = new List<BimStorey>();
	public ICollection<BimSpace> Spaces { get; set; } = new List<BimSpace>();
	public ICollection<BimDoor> Doors { get; set; } = new List<BimDoor>();
	public ICollection<BimWall> Walls { get; set; } = new List<BimWall>();
	public ICollection<AuditFinding> AuditFindings { get; set; } = new List<AuditFinding>();
}