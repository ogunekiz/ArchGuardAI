using ArchGuard.Application.DTOs;

namespace ArchGuard.Application.Interfaces;

public interface IBimProjectService
{
	Task<ProjectSummaryResponseDto> ImportFromIfcAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
	Task<ProjectSummaryResponseDto> ImportFromDirectModelAsync(IfcParseResponseDto modelData, string sourceType = "RevitPlugin", CancellationToken cancellationToken = default);
	Task<List<ProjectSummaryResponseDto>> GetAllProjectsAsync(CancellationToken cancellationToken = default);
	Task<ProjectSummaryResponseDto?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);
}