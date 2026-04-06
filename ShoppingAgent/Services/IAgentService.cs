using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

public interface IAgentService
{
#pragma warning disable MA0046
    event Action OnStateChanged;
#pragma warning restore MA0046

    bool IsProcessing { get; }

    IList<ChatMessage> Messages { get; }

    string SelectedShopKey { get; }

    IReadOnlyList<ShopConfig> AvailableShops { get; }

    Task InitializeAsync(string shopKey = null, CancellationToken cancellationToken = default);

    Task SwitchShopAsync(string shopKey, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ProcessMessageAsync(string userMessage, CancellationToken cancellationToken = default);
}
