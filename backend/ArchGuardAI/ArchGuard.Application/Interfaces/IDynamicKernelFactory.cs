using ArchGuard.Domain.Enums;
using Microsoft.SemanticKernel;

namespace ArchGuard.Application.Interfaces;

public interface IDynamicKernelFactory
{
	Kernel CreateKernel(LlmProviderType providerType, bool includeBimTools = false, Guid? projectId = null);
}