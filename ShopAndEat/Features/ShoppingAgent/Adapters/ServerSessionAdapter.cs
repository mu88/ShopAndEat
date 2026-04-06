using BizDbAccess;
using DataLayer.EF;
using DataLayer.EfClasses;
using DTO.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceLayer;
using ShoppingAgent.Services;

namespace ShopAndEat.Features.ShoppingAgent.Adapters;

/// <summary>
/// Server-side adapter for <see cref="ISessionService"/>.
/// Bypasses the HTTP API and calls <see cref="ISessionRepository"/>,
/// <see cref="IMealService"/>, and <see cref="EfCoreContext"/> directly.
/// </summary>
public partial class ServerSessionAdapter(
    ISessionRepository sessionRepository,
    IMealService mealService,
    EfCoreContext dbContext,
    TimeProvider timeProvider,
    ILogger<ServerSessionAdapter> logger) : ISessionService
{
    public async Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var sessions = await sessionRepository.GetAllSessionsAsync(limit, cancellationToken);
        return sessions.Select(session => new SessionSummary
        {
            SessionId = session.ShoppingSessionId.Value,
            StartedAt = session.StartedAt,
            Status = session.Status.ToString(),
            IngredientList = session.IngredientList,
            ItemCount = session.Items.Count,
        }).ToList();
    }

    public async Task<int> CreateSessionAsync(string ingredientList, CancellationToken cancellationToken = default)
    {
        var session = new ShoppingSession(ingredientList, timeProvider.GetUtcNow());
        var sessionId = await sessionRepository.CreateSessionAsync(session, cancellationToken);
        LogSessionCreated(logger, sessionId.Value);
        return sessionId.Value;
    }

    public async Task AddSessionItemAsync(int sessionId, SessionItemDto item, CancellationToken cancellationToken = default)
    {
        var typedId = new ShoppingSessionId(sessionId);
        var session = await sessionRepository.FindSessionAsync(typedId, cancellationToken);
        if (session is null)
        {
            LogSessionNotFoundForItem(logger, sessionId);
            return;
        }

        var sessionItem = new ShoppingSessionItem(item.OriginalIngredient, typedId, timeProvider.GetUtcNow());
        await sessionRepository.AddItemToSessionAsync(sessionItem, cancellationToken);
    }

    public async Task CompleteSessionAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.FindSessionAsync(new ShoppingSessionId(sessionId), cancellationToken);
        if (session is null)
        {
            LogSessionNotFoundForComplete(logger, sessionId);
            return;
        }

        await sessionRepository.CompleteSessionAsync(session, cancellationToken);
    }

    public async Task<IReadOnlyList<IngredientItem>> GetIngredientListAsync(CancellationToken cancellationToken = default)
    {
        var store = await dbContext.Stores
            .OrderBy(store => store.StoreId)
            .FirstOrDefaultAsync(cancellationToken);

        if (store is null)
        {
            return Array.Empty<IngredientItem>();
        }

        var storeDto = new ExistingStoreDto(store.StoreId, store.Name);
        var purchaseItems = mealService.GetOrderedPurchaseItems(storeDto);

        return purchaseItems.Select(purchaseItem => new IngredientItem
        {
            Text = purchaseItem.ToString(),
            Article = purchaseItem.Article?.Name ?? string.Empty,
            Quantity = purchaseItem.Quantity,
            Unit = purchaseItem.Unit?.Name ?? string.Empty,
        }).ToList();
    }

    public async Task<IEnumerable<string>> GetUnitsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Units
            .OrderBy(unit => unit.Name)
            .Select(unit => unit.Name)
            .ToListAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Shopping session created: {SessionId}")]
    private static partial void LogSessionCreated(ILogger logger, int sessionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Session {SessionId} not found when adding item")]
    private static partial void LogSessionNotFoundForItem(ILogger logger, int sessionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Session {SessionId} not found when completing")]
    private static partial void LogSessionNotFoundForComplete(ILogger logger, int sessionId);
}
