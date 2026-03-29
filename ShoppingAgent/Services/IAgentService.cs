using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

public interface IAgentService
{
    bool IsProcessing { get; }

    List<ChatMessage> Messages { get; }

    event Action OnStateChanged;

    string SelectedShopKey { get; }

    IReadOnlyList<ShopConfig> AvailableShops { get; }

    Task InitializeAsync(string shopKey = null, CancellationToken cancellationToken = default);

    Task SwitchShopAsync(string shopKey, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ProcessMessageAsync(string userMessage, CancellationToken cancellationToken = default);
}
