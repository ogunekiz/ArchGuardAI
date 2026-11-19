namespace ArchGuard.Domain.Entities;

public class BimDoor
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid BimProjectId { get; set; }
	public string GlobalId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string StoreyName { get; set; } = string.Empty;
	public double WidthMeters { get; set; }
	public double HeightMeters { get; set; }
	public bool IsFireExit { get; set; }
	public string FireRating { get; set; } = string.Empty;

	public BimProject BimProject { get; set; } = null!;
}