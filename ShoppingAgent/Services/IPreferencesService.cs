namespace ShoppingAgent.Services;

public interface IPreferencesService
{
    Task<IReadOnlyList<PreferenceDto>> GetAllPreferencesAsync(string storeKey = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PreferenceDto>> GetPreferencesForArticleAsync(string articleName, string storeKey = null, CancellationToken cancellationToken = default);

    Task SavePreferenceAsync(PreferenceDto preference, CancellationToken cancellationToken = default);

    Task<bool> DeletePreferenceAsync(string scope, string key, string storeKey = null, CancellationToken cancellationToken = default);
}
