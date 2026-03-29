using DataLayer.EfClasses;

namespace BizDbAccess;

public interface ISessionRepository
{
    Task<IReadOnlyList<ShoppingSession>> GetAllSessionsAsync(int limit, CancellationToken cancellationToken = default);

    Task<ShoppingSession> GetSessionByIdAsync(ShoppingSessionId id, CancellationToken cancellationToken = default);

    Task<ShoppingSession> FindSessionAsync(ShoppingSessionId id, CancellationToken cancellationToken = default);

    Task<ShoppingSessionId> CreateSessionAsync(ShoppingSession session, CancellationToken cancellationToken = default);

    Task<ShoppingSessionItemId> AddItemToSessionAsync(ShoppingSessionItem item, CancellationToken cancellationToken = default);

    Task CompleteSessionAsync(ShoppingSession session, CancellationToken cancellationToken = default);

    Task DeleteSessionAsync(ShoppingSession session, CancellationToken cancellationToken = default);
}
