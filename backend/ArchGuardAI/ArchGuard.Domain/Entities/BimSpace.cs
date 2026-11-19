namespace ArchGuard.Domain.Entities;

public class BimSpace
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public Guid BimProjectId { get; set; }
	public string GlobalId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Code { get; set; } = string.Empty;
	public string StoreyName { get; set; } = string.Empty;
	public double AreaM2 { get; set; }

	public BimProject BimProject { get; set; } = null!;
}