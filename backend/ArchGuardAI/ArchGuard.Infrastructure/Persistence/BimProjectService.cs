using ArchGuard.Application.DTOs;
using ArchGuard.Application.Interfaces;
using ArchGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchGuard.Infrastructure.Persistence;

public class BimProjectService : IBimProjectService
{
	private readonly AppDbContext _dbContext;
	private readonly IIfcProcessorClient _ifcClient;

	public BimProjectService(AppDbContext dbContext, IIfcProcessorClient ifcClient)
	{
		_dbContext = dbContext;
		_ifcClient = ifcClient;
	}

	public async Task<ProjectSummaryResponseDto> ImportFromIfcAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
	{
		var parsedData = await _ifcClient.ParseIfcStreamAsync(fileStream, fileName, cancellationToken);
		parsedData.SourceFile = fileName;
		return await ImportFromDirectModelAsync(parsedData, "IFC", cancellationToken);
	}

	public async Task<ProjectSummaryResponseDto> ImportFromDirectModelAsync(IfcParseResponseDto modelData, string sourceType = "RevitPlugin", CancellationToken cancellationToken = default)
	{
		var project = new BimProject
		{
			ProjectName = string.IsNullOrWhiteSpace(modelData.ProjectName) ? "Adsız BIM Projesi" : modelData.ProjectName,
			SourceFileName = modelData.SourceFile,
			SourceType = sourceType,
			CreatedAtUtc = DateTime.UtcNow,
			Storeys = modelData.Storeys.Select(s => new BimStorey
			{
				GlobalId = s.GlobalId,
				Name = s.Name,
				Elevation = s.Elevation
			}).ToList(),
			Spaces = modelData.Spaces.Select(sp => new BimSpace
			{
				GlobalId = sp.GlobalId,
				Name = sp.Name,
				Code = sp.Code,
				StoreyName = sp.Storey,
				AreaM2 = sp.AreaM2
			}).ToList(),
			Doors = modelData.Doors.Select(d => new BimDoor
			{
				GlobalId = d.GlobalId,
				Name = d.Name,
				StoreyName = d.Storey,
				WidthMeters = d.WidthM,
				HeightMeters = d.HeightM,
				IsFireExit = d.IsFireExit,
				FireRating = d.FireRating
			}).ToList(),
			Walls = modelData.Walls.Select(w => new BimWall
			{
				GlobalId = w.GlobalId,
				Name = w.Name,
				StoreyName = w.Storey,
				IsExternal = w.IsExternal,
				LengthMeters = w.LengthM,
				AreaM2 = w.AreaM2,
				VolumeM3 = w.VolumeM3
			}).ToList()
		};

		_dbContext.BimProjects.Add(project);
		await _dbContext.SaveChangesAsync(cancellationToken);

		return MapToSummary(project);
	}

	public async Task<List<ProjectSummaryResponseDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default)
	{
		var projects = await _dbContext.BimProjects
				.Include(p => p.Storeys)
				.Include(p => p.Spaces)
				.Include(p => p.Doors)
				.Include(p => p.Walls)
				.OrderByDescending(p => p.CreatedAtUtc)
				.ToListAsync(cancellationToken);

		return projects.Select(MapToSummary).ToList();
	}

	public async Task<ProjectSummaryResponseDto?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
	{
		var project = await _dbContext.BimProjects
				.Include(p => p.Storeys)
				.Include(p => p.Spaces)
				.Include(p => p.Doors)
				.Include(p => p.Walls)
				.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

		return project is null ? null : MapToSummary(project);
	}

	private static ProjectSummaryResponseDto MapToSummary(BimProject p) => new()
	{
		ProjectId = p.Id,
		ProjectName = p.ProjectName,
		SourceFileName = p.SourceFileName,
		SourceType = p.SourceType,
		CreatedAtUtc = p.CreatedAtUtc,
		StoreyCount = p.Storeys.Count,
		SpaceCount = p.Spaces.Count,
		DoorCount = p.Doors.Count,
		WallCount = p.Walls.Count,
		TotalWallAreaM2 = Math.Round(p.Walls.Sum(w => w.AreaM2), 2),
		TotalWallVolumeM3 = Math.Round(p.Walls.Sum(w => w.VolumeM3), 2)
	};
}