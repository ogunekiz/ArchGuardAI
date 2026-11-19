using ArchGuard.Application.Interfaces;
using ArchGuard.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace ArchGuard.Infrastructure.AI;

public class DynamicKernelFactory : IDynamicKernelFactory
{
	private readonly IConfiguration _configuration;
	private readonly IServiceProvider _serviceProvider;

	public DynamicKernelFactory(IConfiguration configuration, IServiceProvider serviceProvider)
	{
		_configuration = configuration;
		_serviceProvider = serviceProvider;
	}

	public Kernel CreateKernel(LlmProviderType providerType, bool includeBimTools = false, Guid? projectId = null)
	{
		var sectionName = providerType == LlmProviderType.Gemini ? "AiProviders:Gemini" : "AiProviders:Nvidia";

		var apiKey = _configuration[$"{sectionName}:ApiKey"]
				?? throw new InvalidOperationException($"{sectionName}:ApiKey yapılandırması bulunamadı.");
		var modelId = _configuration[$"{sectionName}:ModelId"]
				?? throw new InvalidOperationException($"{sectionName}:ModelId yapılandırması bulunamadı.");
		var endpoint = _configuration[$"{sectionName}:Endpoint"]
				?? throw new InvalidOperationException($"{sectionName}:Endpoint yapılandırması bulunamadı.");

		if (apiKey.StartsWith("BURAYA_"))
			throw new InvalidOperationException($"Lütfen appsettings.Development.json içindeki {sectionName}:ApiKey alanına gerçek API anahtarınızı girin.");

		var builder = Kernel.CreateBuilder();

		// 10 dakikalık timeout koruması sağlayan özel HttpClient
		var customHttpClient = new HttpClient
		{
			Timeout = TimeSpan.FromMinutes(10)
		};

		// Hem Gemini hem NVIDIA NIM OpenAI-compatible Chat Completion protokolünü destekler
		builder.AddOpenAIChatCompletion(
				modelId: modelId,
				apiKey: apiKey,
				endpoint: new Uri(endpoint),
				httpClient: customHttpClient);

		if (includeBimTools && projectId.HasValue)
		{
			var dbContext = _serviceProvider.GetRequiredService<Persistence.AppDbContext>();
			builder.Plugins.AddFromObject(new BimQueryPlugin(dbContext, projectId.Value), "BimModelTools");
		}

		return builder.Build();
	}
}