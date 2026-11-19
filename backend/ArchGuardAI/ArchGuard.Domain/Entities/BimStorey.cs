namespace ArchGuard.Domain.Entities;

public class BimStorey
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid BimProjectId { get; set; }
	public string GlobalId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public double Elevation { get; set; }

	public BimProject BimProject { get; set; } = null!;
}