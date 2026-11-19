using System.ComponentModel;
using System.Text.Json;
using ArchGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace ArchGuard.Infrastructure.AI;

public class BimQueryPlugin
{
	private readonly AppDbContext _dbContext;
	private readonly Guid _projectId;

	public BimQueryPlugin(AppDbContext dbContext, Guid projectId)
	{
		_dbContext = dbContext;
		_projectId = projectId;
	}

	[KernelFunction("get_project_overview")]
	[Description("Projenin genel özetini, kat listesini, toplam oda, kapı ve duvar sayılarını getirir.")]
	public async Task<string> GetProjectOverviewAsync()
	{
		var project = await _dbContext.BimProjects
				.Include(p => p.Storeys)
				.FirstOrDefaultAsync(p => p.Id == _projectId);

		if (project is null) return "Proje bulunamadı.";

		var overview = new
		{
			project.ProjectName,
			project.SourceType,
			Storeys = project.Storeys.Select(s => new { s.Name, s.Elevation }),
			DoorCount = await _dbContext.BimDoors.CountAsync(d => d.BimProjectId == _projectId),
			SpaceCount = await _dbContext.BimSpaces.CountAsync(s => s.BimProjectId == _projectId),
			WallCount = await _dbContext.BimWalls.CountAsync(w => w.BimProjectId == _projectId)
		};

		return JsonSerializer.Serialize(overview);
	}

	[KernelFunction("get_doors_info")]
	[Description("Projedeki kapıların listesini, hangi katta olduklarını, genişlik (metre), yükseklik (metre) ve yangın çıkışı bilgilerini döner.")]
	public async Task<string> GetDoorsInfoAsync(
			[Description("İsteğe bağlı kat adı filtresi (boş bırakılırsa tüm kapılar gelir)")] string? storeyName = null)
	{
		var query = _dbContext.BimDoors.Where(d => d.BimProjectId == _projectId);

		if (!string.IsNullOrWhiteSpace(storeyName))
			query = query.Where(d => d.StoreyName.ToLower().Contains(storeyName.ToLower()));

		var doors = await query.Select(d => new
		{
			d.GlobalId,
			d.Name,
			d.StoreyName,
			d.WidthMeters,
			d.HeightMeters,
			d.IsFireExit,
			d.FireRating
		}).ToListAsync();

		return JsonSerializer.Serialize(doors);
	}

	[KernelFunction("get_spaces_info")]
	[Description("Projedeki odaların/mahallerin listesini, kat bilgilerini ve net metrekare (AreaM2) alanlarını getirir.")]
	public async Task<string> GetSpacesInfoAsync()
	{
		var spaces = await _dbContext.BimSpaces
				.Where(s => s.BimProjectId == _projectId)
				.Select(s => new
				{
					s.GlobalId,
					s.Name,
					s.Code,
					s.StoreyName,
					s.AreaM2
				}).ToListAsync();

		return JsonSerializer.Serialize(spaces);
	}

	[KernelFunction("calculate_wall_quantities")]
	[Description("Projedeki duvarların metrajlarını (uzunluk, toplam alan m2 ve toplam hacim m3) iç/dış duvar ve kat bazında özetler.")]
	public async Task<string> CalculateWallQuantitiesAsync(
			[Description("Sadece dış duvarları hesaplamak için true, sadece iç duvarlar için false, tümü için null gönderin")] bool? onlyExternal = null)
	{
		var query = _dbContext.BimWalls.Where(w => w.BimProjectId == _projectId);

		if (onlyExternal.HasValue)
			query = query.Where(w => w.IsExternal == onlyExternal.Value);

		var walls = await query.ToListAsync();

		var summary = new
		{
			Filter = onlyExternal.HasValue ? (onlyExternal.Value ? "Dış Duvarlar" : "İç Duvarlar") : "Tüm Duvarlar",
			WallCount = walls.Count,
			TotalLengthMeters = Math.Round(walls.Sum(w => w.LengthMeters), 2),
			TotalAreaM2 = Math.Round(walls.Sum(w => w.AreaM2), 2),
			TotalVolumeM3 = Math.Round(walls.Sum(w => w.VolumeM3), 2),
			ByStorey = walls.GroupBy(w => w.StoreyName).Select(g => new
			{
				Storey = g.Key,
				Count = g.Count(),
				AreaM2 = Math.Round(g.Sum(x => x.AreaM2), 2),
				VolumeM3 = Math.Round(g.Sum(x => x.VolumeM3), 2)
			})
		};

		return JsonSerializer.Serialize(summary);
	}

	[KernelFunction("get_existing_audit_findings")]
	[Description("Proje için daha önce yapılmış yönetmelik denetimi hata ve uyarı bulgularını getirir.")]
	public async Task<string> GetExistingAuditFindingsAsync()
	{
		var findings = await _dbContext.AuditFindings
				.Where(f => f.BimProjectId == _projectId)
				.Select(f => new
				{
					f.ElementType,
					f.ElementName,
					f.StoreyName,
					Severity = f.Severity.ToString(),
					f.RuleReference,
					f.Description,
					f.SuggestedFix
				}).ToListAsync();

		return findings.Count == 0
				? "Bu proje için henüz çalıştırılmış bir yönetmelik denetim raporu bulunmuyor."
				: JsonSerializer.Serialize(findings);
	}
}