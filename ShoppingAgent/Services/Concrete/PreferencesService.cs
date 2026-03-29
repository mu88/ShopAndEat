using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using ShoppingAgent.Logging;
using ShoppingAgent.Services;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Manages shopping preferences via the ShopAndEat API.
/// Preferences are shared across all household members.
/// </summary>
public class PreferencesService : IPreferencesService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PreferencesService> _logger;

    public PreferencesService(HttpClient httpClient, ILogger<PreferencesService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PreferenceDto>> GetAllPreferencesAsync(string storeKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(storeKey))
            {
                query["storeKey"] = storeKey;
            }

            var url = query.Count > 0 ? $"api/preferences?{query}" : "api/preferences";
            return await _httpClient.GetFromJsonAsync<List<PreferenceDto>>(url, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            ServiceLogMessages.GetPreferencesFailed(_logger, storeKey ?? string.Empty, ex.Message);
            return [];
        }
    }

    public async Task<IReadOnlyList<PreferenceDto>> GetPreferencesForArticleAsync(string articleName, string storeKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["scope"] = $"article:{articleName}";
            if (!string.IsNullOrEmpty(storeKey))
            {
                query["storeKey"] = storeKey;
            }

            return await _httpClient.GetFromJsonAsync<List<PreferenceDto>>($"api/preferences?{query}", cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            ServiceLogMessages.GetPreferencesForArticleFailed(_logger, articleName, ex.Message);
            return [];
        }
    }

    public async Task SavePreferenceAsync(PreferenceDto preference, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/preferences", preference, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> DeletePreferenceAsync(string scope, string key, string storeKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["scope"] = scope;
            query["key"] = key;
            if (!string.IsNullOrEmpty(storeKey))
            {
                query["storeKey"] = storeKey;
            }

            var response = await _httpClient.DeleteAsync($"api/preferences?{query}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            ServiceLogMessages.DeletePreferenceFailed(_logger, scope, key, ex.Message);
            return false;
        }
    }
}

