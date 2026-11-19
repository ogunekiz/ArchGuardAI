using System.Text.Json;
using System.Text.Json.Serialization;
using ArchGuard.Infrastructure.AI;
using ArchGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchGuard.Api.Controllers;

/// <summary>
/// Model Context Protocol (MCP) JSON-RPC 2.0 uyumlu BIM Tool Server endpoint'i.
/// Harici LLM istemcilerinin (Claude Desktop vb.) doğrudan BIM veritabanını sorgulamasını sağlar.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class McpController : ControllerBase
{
	private readonly AppDbContext _dbContext;

	public McpController(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	[HttpPost]
	public async Task<IActionResult> HandleRpc([FromBody] McpJsonRpcRequest request, CancellationToken cancellationToken)
	{
		if (request.Method == "initialize")
		{
			return Ok(new
			{
				jsonrpc = "2.0",
				id = request.Id,
				result = new
				{
					protocolVersion = "2024-11-05",
					serverInfo = new { name = "ArchGuard-BIM-MCP-Server", version = "1.0.0" },
					capabilities = new { tools = new { } }
				}
			});
		}

		if (request.Method == "tools/list")
		{
			var tools = new object[]
			{
								new
								{
										name = "get_project_overview",
										description = "En güncel veya belirtilen BIM projesinin kat, oda, kapı ve duvar özetini döner.",
										inputSchema = new
										{
												type = "object",
												properties = new
												{
														projectId = new { type = "string", description = "Opsiyonel Proje GUID değeri" }
												}
										}
								},
								new
								{
										name = "get_doors_info",
										description = "BIM projesindeki kapıların genişlik, yükseklik ve yangın çıkışı bilgilerini döner.",
										inputSchema = new
										{
												type = "object",
												properties = new
												{
														projectId = new { type = "string", description = "Opsiyonel Proje GUID değeri" },
														storeyName = new { type = "string", description = "Kat adı filtresi" }
												}
										}
								},
								new
								{
										name = "calculate_wall_quantities",
										description = "Dış ve iç duvarların toplam uzunluk, alan (m2) ve hacim (m3) metrajlarını hesaplar.",
										inputSchema = new
										{
												type = "object",
												properties = new
												{
														projectId = new { type = "string", description = "Opsiyonel Proje GUID değeri" },
														onlyExternal = new { type = "boolean", description = "Sadece dış duvarlar için true" }
												}
										}
								}
			};

			return Ok(new { jsonrpc = "2.0", id = request.Id, result = new { tools } });
		}

		if (request.Method == "tools/call")
		{
			var toolName = request.Params?.GetProperty("name").GetString() ?? string.Empty;
			JsonElement? args = request.Params.HasValue && request.Params.Value.TryGetProperty("arguments", out var a) ? a : null;

			Guid? targetProjectId = null;
			if (args.HasValue && args.Value.TryGetProperty("projectId", out var pidProp) && Guid.TryParse(pidProp.GetString(), out var parsedId))
			{
				targetProjectId = parsedId;
			}

			// Eğer projectId verilmediyse en son yüklenen projeyi otomatik seç
			targetProjectId ??= await _dbContext.BimProjects
					.OrderByDescending(p => p.CreatedAtUtc)
					.Select(p => (Guid?)p.Id)
					.FirstOrDefaultAsync(cancellationToken);

			if (!targetProjectId.HasValue)
			{
				return Ok(new
				{
					jsonrpc = "2.0",
					id = request.Id,
					error = new { code = -32001, message = "Sistemde kayıtlı BIM projesi bulunamadı." }
				});
			}

			var plugin = new BimQueryPlugin(_dbContext, targetProjectId.Value);

			string outputText = toolName switch
			{
				"get_project_overview" => await plugin.GetProjectOverviewAsync(),
				"get_doors_info" => await plugin.GetDoorsInfoAsync(
						args.HasValue && args.Value.TryGetProperty("storeyName", out var st) ? st.GetString() : null),
				"calculate_wall_quantities" => await plugin.CalculateWallQuantitiesAsync(
						args.HasValue && args.Value.TryGetProperty("onlyExternal", out var ext) && ext.ValueKind is JsonValueKind.True or JsonValueKind.False
								? ext.GetBoolean()
								: null),
				_ => throw new InvalidOperationException($"Bilinmeyen MCP aracı: {toolName}")
			};

			return Ok(new
			{
				jsonrpc = "2.0",
				id = request.Id,
				result = new
				{
					content = new[]
							{
												new { type = "text", text = outputText }
										}
				}
			});
		}

		return BadRequest(new { jsonrpc = "2.0", id = request.Id, error = new { code = -32601, message = "Desteklenmeyen MCP metodu." } });
	}

	public sealed class McpJsonRpcRequest
	{
		[JsonPropertyName("jsonrpc")]
		public string JsonRpc { get; set; } = "2.0";

		[JsonPropertyName("id")]
		public object? Id { get; set; }

		[JsonPropertyName("method")]
		public string Method { get; set; } = string.Empty;

		[JsonPropertyName("params")]
		public JsonElement? Params { get; set; }
	}
}