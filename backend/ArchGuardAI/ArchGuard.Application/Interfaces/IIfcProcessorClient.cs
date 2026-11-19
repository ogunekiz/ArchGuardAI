using ArchGuard.Application.DTOs;

namespace ArchGuard.Application.Interfaces;

public interface IIfcProcessorClient
{
	Task<IfcParseResponseDto> ParseIfcStreamAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}