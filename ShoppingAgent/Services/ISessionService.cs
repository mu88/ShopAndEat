namespace ShoppingAgent.Services;

public interface ISessionService
{
    Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(int limit = 20, CancellationToken cancellationToken = default);

    Task<int> CreateSessionAsync(string ingredientList, CancellationToken cancellationToken = default);

    Task AddSessionItemAsync(int sessionId, SessionItemDto item, CancellationToken cancellationToken = default);

    Task CompleteSessionAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IngredientItem>> GetIngredientListAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetUnitsAsync(CancellationToken cancellationToken = default);
}
