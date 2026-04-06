using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

public interface IExtensionBridge : IAsyncDisposable
{
#pragma warning disable MA0046
    event Action OnConnectionChanged;
#pragma warning restore MA0046

    bool IsExtensionConnected { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<ToolResult> ExecuteToolAsync(string toolName, IDictionary<string, object> arguments, string shopKey, CancellationToken cancellationToken = default);
}
