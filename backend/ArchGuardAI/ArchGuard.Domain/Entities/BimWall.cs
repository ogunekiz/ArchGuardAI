namespace ArchGuard.Domain.Entities;

public class BimWall
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid BimProjectId { get; set; }
	public string GlobalId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string StoreyName { get; set; } = string.Empty;
	public bool IsExternal { get; set; }
	public double LengthMeters { get; set; }
	public double AreaM2 { get; set; }
	public double VolumeM3 { get; set; }

	public BimProject BimProject { get; set; } = null!;
}