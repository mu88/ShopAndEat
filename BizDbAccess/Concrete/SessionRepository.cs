using DataLayer.EF;
using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;

namespace BizDbAccess.Concrete;

public class SessionRepository(EfCoreContext context, TimeProvider timeProvider) : ISessionRepository
{
    public async Task<IReadOnlyList<ShoppingSession>> GetAllSessionsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await context.ShoppingSessions
            .Include(session => session.Items)
            .OrderByDescending(session => session.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShoppingSession> GetSessionByIdAsync(ShoppingSessionId id, CancellationToken cancellationToken = default)
    {
        return await context.ShoppingSessions
            .Include(session => session.Items)
            .FirstOrDefaultAsync(session => session.ShoppingSessionId == id, cancellationToken);
    }

    public async Task<ShoppingSession> FindSessionAsync(ShoppingSessionId id, CancellationToken cancellationToken = default)
    {
        return await context.ShoppingSessions.FindAsync([id], cancellationToken);
    }

    public async Task<ShoppingSessionId> CreateSessionAsync(ShoppingSession session, CancellationToken cancellationToken = default)
    {
        context.ShoppingSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
        return session.ShoppingSessionId;
    }

    public async Task<ShoppingSessionItemId> AddItemToSessionAsync(ShoppingSessionItem item, CancellationToken cancellationToken = default)
    {
        context.ShoppingSessionItems.Add(item);
        await context.SaveChangesAsync(cancellationToken);
        return item.ShoppingSessionItemId;
    }

    public async Task CompleteSessionAsync(ShoppingSession session, CancellationToken cancellationToken = default)
    {
        session.Status = SessionStatus.Completed;
        session.CompletedAt = timeProvider.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSessionAsync(ShoppingSession session, CancellationToken cancellationToken = default)
    {
        context.ShoppingSessions.Remove(session);
        await context.SaveChangesAsync(cancellationToken);
    }
}
