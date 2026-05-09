using Refit;
using System.Text.Json;

namespace Kernel.Contracts.Abstractions;

public interface IKernelCommandsApi
{
    [Post("/commands/handle")]
    Task<string> HandleAsync([Body] JsonElement command, CancellationToken ct);
}
