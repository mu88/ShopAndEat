namespace ShoppingAgent.Services;

public record PreferenceDto
{
    public string Scope { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string StoreKey { get; init; }
}
