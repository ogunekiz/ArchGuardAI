using ArchGuard.Application.DTOs;
using ArchGuard.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ArchGuard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
	private readonly IBimProjectService _projectService;

	public ProjectsController(IBimProjectService projectService)
	{
		_projectService = projectService;
	}

	[HttpPost("upload-ifc")]
	[ProducesResponseType(typeof(ProjectSummaryResponseDto), StatusCodes.Status200OK)]
	public async Task<IActionResult> UploadIfc(IFormFile file, CancellationToken cancellationToken)
	{
		if (file is null || file.Length == 0)
			return BadRequest("Lütfen geçerli bir .ifc dosyası seçin.");

		if (!file.FileName.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase))
			return BadRequest("Sadece .ifc uzantılı dosyalar desteklenmektedir.");

		await using var stream = file.OpenReadStream();
		var result = await _projectService.ImportFromIfcAsync(stream, file.FileName, cancellationToken);
		return Ok(result);
	}

	/// <summary>
	/// Revit eklentisinden (Plugin) doğrudan JSON olarak gönderilen model verisini içeri alır.
	/// </summary>
	[HttpPost("ingest-from-revit")]
	[ProducesResponseType(typeof(ProjectSummaryResponseDto), StatusCodes.Status200OK)]
	public async Task<IActionResult> IngestFromRevit([FromBody] IfcParseResponseDto modelData, CancellationToken cancellationToken)
	{
		var result = await _projectService.ImportFromDirectModelAsync(modelData, "RevitPlugin", cancellationToken);
		return Ok(result);
	}

	[HttpGet]
	[ProducesResponseType(typeof(List<ProjectSummaryResponseDto>), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
	{
		var projects = await _projectService.GetAllProjectsAsync(cancellationToken);
		return Ok(projects);
	}

	[HttpGet("{id:guid}")]
	[ProducesResponseType(typeof(ProjectSummaryResponseDto), StatusCodes.Status200OK)]
	public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
	{
		var project = await _projectService.GetProjectByIdAsync(id, cancellationToken);
		return project is null ? NotFound() : Ok(project);
	}
}