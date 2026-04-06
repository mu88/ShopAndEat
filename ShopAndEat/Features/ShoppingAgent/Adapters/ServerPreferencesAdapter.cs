using BizDbAccess;
using DataLayer.EfClasses;
using Microsoft.Extensions.Logging;
using ShoppingAgent.Services;

namespace ShopAndEat.Features.ShoppingAgent.Adapters;

/// <summary>
/// Server-side adapter for <see cref="IPreferencesService"/>.
/// Bypasses the HTTP API and calls <see cref="IPreferencesRepository"/> directly.
/// </summary>
public partial class ServerPreferencesAdapter(IPreferencesRepository repository, ILogger<ServerPreferencesAdapter> logger) : IPreferencesService
{
    public async Task<IReadOnlyList<PreferenceDto>> GetAllPreferencesAsync(string storeKey = null, CancellationToken cancellationToken = default)
    {
        var preferences = await repository.GetAllPreferencesAsync(null, storeKey, cancellationToken);
        return preferences.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<PreferenceDto>> GetPreferencesForArticleAsync(string articleName, string storeKey = null, CancellationToken cancellationToken = default)
    {
        var scope = $"article:{articleName}";
        var preferences = await repository.GetAllPreferencesAsync(scope, storeKey, cancellationToken);
        return preferences.Select(ToDto).ToList();
    }

    public async Task SavePreferenceAsync(PreferenceDto preference, CancellationToken cancellationToken = default)
    {
        var entity = new ShoppingPreference(preference.Scope, preference.Key, PreferenceSource.AgentLearned, preference.StoreKey)
        {
            Value = preference.Value,
        };
        await repository.UpsertPreferenceAsync(entity, cancellationToken);
        LogPreferenceSaved(logger, preference.Scope, preference.Key);
    }

    public Task<bool> DeletePreferenceAsync(string scope, string key, string storeKey = null, CancellationToken cancellationToken = default) =>
        repository.DeletePreferenceAsync(scope, key, storeKey, cancellationToken);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Preference saved: [{Scope}] {Key}")]
    private static partial void LogPreferenceSaved(ILogger logger, string scope, string key);

    private static PreferenceDto ToDto(ShoppingPreference preference) =>
        new()
        {
            Scope = preference.Scope,
            Key = preference.Key,
            Value = preference.Value ?? string.Empty,
            StoreKey = preference.StoreKey,
        };
}
