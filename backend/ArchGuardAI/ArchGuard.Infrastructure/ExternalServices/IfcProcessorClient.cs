using System.Net.Http.Headers;
using System.Text.Json;
using ArchGuard.Application.DTOs;
using ArchGuard.Application.Interfaces;

namespace ArchGuard.Infrastructure.ExternalServices;

public class IfcProcessorClient : IIfcProcessorClient
{
	private readonly HttpClient _httpClient;

	public IfcProcessorClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<IfcParseResponseDto> ParseIfcStreamAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
	{
		using var content = new MultipartFormDataContent();
		using var streamContent = new StreamContent(fileStream);
		streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

		content.Add(streamContent, "file", fileName);

		var response = await _httpClient.PostAsync("/parse-ifc", content, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new InvalidOperationException($"Python IFC servisi hata döndü ({response.StatusCode}): {errorBody}");
		}

		var json = await response.Content.ReadAsStringAsync(cancellationToken);
		var result = JsonSerializer.Deserialize<IfcParseResponseDto>(json);

		return result ?? throw new InvalidOperationException("Python IFC servisinden boş yanıt döndü.");
	}
}