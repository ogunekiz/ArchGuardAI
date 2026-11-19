using ArchGuard.Application.DTOs;
using ArchGuard.Domain.Enums;

namespace ArchGuard.Application.Interfaces;

public interface IBimAuditService
{
	Task<List<AuditFindingResponseDto>> RunComplianceAuditAsync(Guid projectId, LlmProviderType provider, CancellationToken cancellationToken = default);
	Task<List<AuditFindingResponseDto>> GetProjectFindingsAsync(Guid projectId, CancellationToken cancellationToken = default);
	Task<BimChatResponseDto> ChatWithBimModelAsync(Guid projectId, string userMessage, LlmProviderType provider, CancellationToken cancellationToken = default);
}