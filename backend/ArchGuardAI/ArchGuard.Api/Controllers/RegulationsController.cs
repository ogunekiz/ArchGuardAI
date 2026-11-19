using ArchGuard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ArchGuard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegulationsController : ControllerBase
{
	private readonly IRegulationRagService _ragService;

	public RegulationsController(IRegulationRagService ragService)
	{
		_ragService = ragService;
	}

	/// <summary>
	/// Özel bir yönetmelik veya şirket içi BIM standardı PDF dosyasını Qdrant'a indeksler.
	/// </summary>
	[HttpPost("upload-pdf")]
	public async Task<IActionResult> UploadRegulationPdf(IFormFile file, [FromQuery] string category = "Genel Yönetmelik", CancellationToken cancellationToken = default)
	{
		if (file is null || file.Length == 0)
			return BadRequest("Lütfen geçerli bir PDF dosyası seçin.");

		await using var stream = file.OpenReadStream();
		await _ragService.IngestPdfAsync(stream, category, cancellationToken);
		return Ok(new { message = $"{file.FileName} başarıyla parçalandı ve Qdrant'a indekslendi." });
	}

	/// <summary>
	/// Vektör veritabanında anlamsal yönetmelik araması (RAG Retrieval) yapar.
	/// </summary>
	[HttpGet("search")]
	public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int topK = 3, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(query))
			return BadRequest("Arama sorgusu boş olamaz.");

		var results = await _ragService.SearchRelevantRulesAsync(query, topK, cancellationToken);
		return Ok(results);
	}
}