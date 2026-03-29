using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

public interface IExtensionBridge : IAsyncDisposable
{
    bool IsExtensionConnected { get; }

    event Action OnConnectionChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<ToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> arguments, string shopKey, CancellationToken cancellationToken = default);
}
