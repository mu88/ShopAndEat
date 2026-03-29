namespace ShoppingAgent.Services;

public interface ISystemPromptBuilder
{
    Task<string> BuildSystemPromptAsync(string shopName, string shopUrl, string shopKey, CancellationToken ct = default);
}
