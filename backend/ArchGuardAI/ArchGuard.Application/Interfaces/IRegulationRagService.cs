using ArchGuard.Application.DTOs;

namespace ArchGuard.Application.Interfaces;

public interface IRegulationRagService
{
	Task IngestPdfAsync(Stream pdfStream, string category, CancellationToken cancellationToken = default);
	Task<List<RegulationSearchResultDto>> SearchRelevantRulesAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}