using ArchGuard.Application.DTOs;
using ArchGuard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ArchGuard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
	private readonly IBimAuditService _auditService;

	public AuditController(IBimAuditService auditService)
	{
		_auditService = auditService;
	}

	/// <summary>
	/// Seçilen BIM projesini seçilen LLM sağlayıcı (1: Gemini, 2: Nvidia) ve Qdrant yönetmelikleriyle denetler.
	/// </summary>
	[HttpPost("run")]
	[ProducesResponseType(typeof(List<AuditFindingResponseDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> RunAudit([FromBody] RunAuditRequestDto request, CancellationToken cancellationToken)
	{
		var findings = await _auditService.RunComplianceAuditAsync(request.ProjectId, request.Provider, cancellationToken);
		return Ok(findings);
	}

	/// <summary>
	/// Projenin veritabanında kayıtlı denetim bulgularını getirir.
	/// </summary>
	[HttpGet("{projectId:guid}")]
	[ProducesResponseType(typeof(List<AuditFindingResponseDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetFindings(Guid projectId, CancellationToken cancellationToken)
	{
		var findings = await _auditService.GetProjectFindingsAsync(projectId, cancellationToken);
		return Ok(findings);
	}

	/// <summary>
	/// Tool Calling kullanarak BIM modeliyle doğal dilde sohbet eder ve metraj/eleman sorgular.
	/// </summary>
	[HttpPost("chat")]
	[ProducesResponseType(typeof(BimChatResponseDto), StatusCodes.Status200OK)]
	public async Task<IActionResult> ChatWithModel([FromBody] BimChatRequestDto request, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Message))
			return BadRequest("Mesaj boş olamaz.");

		var reply = await _auditService.ChatWithBimModelAsync(request.ProjectId, request.Message, request.Provider, cancellationToken);
		return Ok(reply);
	}
}