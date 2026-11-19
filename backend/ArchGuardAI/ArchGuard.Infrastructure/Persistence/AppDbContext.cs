using ArchGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchGuard.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	public DbSet<BimProject> BimProjects => Set<BimProject>();
	public DbSet<BimStorey> BimStoreys => Set<BimStorey>();
	public DbSet<BimSpace> BimSpaces => Set<BimSpace>();
	public DbSet<BimDoor> BimDoors => Set<BimDoor>();
	public DbSet<BimWall> BimWalls => Set<BimWall>();
	public DbSet<AuditFinding> AuditFindings => Set<AuditFinding>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<BimProject>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.Property(x => x.ProjectName).HasMaxLength(200).IsRequired();
			entity.Property(x => x.SourceFileName).HasMaxLength(255);
			entity.Property(x => x.SourceType).HasMaxLength(50);

			entity.HasMany(x => x.Storeys).WithOne(x => x.BimProject).HasForeignKey(x => x.BimProjectId).OnDelete(DeleteBehavior.Cascade);
			entity.HasMany(x => x.Spaces).WithOne(x => x.BimProject).HasForeignKey(x => x.BimProjectId).OnDelete(DeleteBehavior.Cascade);
			entity.HasMany(x => x.Doors).WithOne(x => x.BimProject).HasForeignKey(x => x.BimProjectId).OnDelete(DeleteBehavior.Cascade);
			entity.HasMany(x => x.Walls).WithOne(x => x.BimProject).HasForeignKey(x => x.BimProjectId).OnDelete(DeleteBehavior.Cascade);
			entity.HasMany(x => x.AuditFindings).WithOne(x => x.BimProject).HasForeignKey(x => x.BimProjectId).OnDelete(DeleteBehavior.Cascade);
		});
	}
}