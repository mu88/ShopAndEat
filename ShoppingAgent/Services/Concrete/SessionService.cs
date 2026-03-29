using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using ShoppingAgent.Logging;
using ShoppingAgent.Services;

namespace ShoppingAgent.Services.Concrete;

/// <summary>
/// Manages shopping session history via the ShopAndEat API.
/// </summary>
public class SessionService : ISessionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SessionService> _logger;

    public SessionService(HttpClient httpClient, ILogger<SessionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["limit"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return await _httpClient.GetFromJsonAsync<List<SessionSummary>>($"api/shopping/sessions?{query}", cancellationToken)
                ?? [];
        }
        catch (Exception ex)
        {
            ServiceLogMessages.GetSessionsFailed(_logger, ex.Message);
            return [];
        }
    }

    public async Task<int> CreateSessionAsync(string ingredientList, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/shopping/sessions", new { ingredientList }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateSessionResult>(cancellationToken);
        return result?.ShoppingSessionId ?? 0;
    }

    public async Task AddSessionItemAsync(int sessionId, SessionItemDto item, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/shopping/sessions/{sessionId}/items", item, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task CompleteSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsync($"api/shopping/sessions/{sessionId}/complete", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<IngredientItem>> GetIngredientListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IngredientListResult>("api/shopping/ingredients", cancellationToken);
            return response?.Items ?? [];
        }
        catch (Exception ex)
        {
            ServiceLogMessages.GetIngredientListFailed(_logger, ex.Message);
            return [];
        }
    }

    public async Task<IEnumerable<string>> GetUnitsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<string>>("api/units", cancellationToken)
                ?? [];
        }
        catch (Exception ex)
        {
            ServiceLogMessages.GetUnitsFailed(_logger, ex.Message);
            return [];
        }
    }
}

internal class CreateSessionResult
{
    public int ShoppingSessionId { get; set; }
}

internal class IngredientListResult
{
    public List<IngredientItem> Items { get; set; } = new();
}
