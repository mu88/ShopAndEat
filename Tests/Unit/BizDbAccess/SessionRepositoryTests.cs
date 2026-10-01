using BizDbAccess.Concrete;
using DataLayer.EF;
using DataLayer.EfClasses;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizDbAccess;

[TestFixture]
[Category("Unit")]
public class SessionRepositoryTests
{
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero));

    private static DbContextOptions<EfCoreContext> CreateSharedDbOptions(string dbName)
        => new DbContextOptionsBuilder<EfCoreContext>().UseInMemoryDatabase(dbName).Options;

    [Test]
    public async Task GetAllSessionsAsync_ReturnsSessionsOrderedByStartedAtDescending_LimitedAndIncludingItems()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var older = context.ShoppingSessions.Add(new ShoppingSession("Milch", _timeProvider.GetUtcNow().AddDays(-2))).Entity;
        var newer = context.ShoppingSessions.Add(new ShoppingSession("Eier", _timeProvider.GetUtcNow())).Entity;
        context.ShoppingSessions.Add(new ShoppingSession("Brot", _timeProvider.GetUtcNow().AddDays(-5)));
        await context.SaveChangesAsync();
        var item = new ShoppingSessionItem("Milch", newer.ShoppingSessionId, _timeProvider.GetUtcNow());
        context.ShoppingSessionItems.Add(item);
        await context.SaveChangesAsync();
        var testee = new SessionRepository(context, _timeProvider);

        // Act
        var result = await testee.GetAllSessionsAsync(2);

        // Assert
        result.Select(session => session.ShoppingSessionId).Should().Equal(newer.ShoppingSessionId, older.ShoppingSessionId);
        result.Single(session => session.ShoppingSessionId == newer.ShoppingSessionId).Items.Should().ContainSingle(i => i.OriginalIngredient == "Milch");
    }

    [Test]
    public async Task GetSessionByIdAsync_WhenSessionExists_ReturnsSessionWithItems()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var session = context.ShoppingSessions.Add(new ShoppingSessionBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();
        context.ShoppingSessionItems.Add(new ShoppingSessionItem("Milch", session.ShoppingSessionId, _timeProvider.GetUtcNow()));
        await context.SaveChangesAsync();
        var testee = new SessionRepository(context, _timeProvider);

        // Act
        var result = await testee.GetSessionByIdAsync(session.ShoppingSessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle(item => item.OriginalIngredient == "Milch");
    }

    [Test]
    public async Task GetSessionByIdAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new SessionRepository(context, _timeProvider);

        // Act
        var result = await testee.GetSessionByIdAsync(new ShoppingSessionId(999));

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task FindSessionAsync_WhenSessionExists_ReturnsSession()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var session = context.ShoppingSessions.Add(new ShoppingSessionBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();
        var testee = new SessionRepository(context, _timeProvider);

        // Act
        var result = await testee.FindSessionAsync(session.ShoppingSessionId);

        // Assert
        result.Should().NotBeNull();
        result!.ShoppingSessionId.Should().Be(session.ShoppingSessionId);
    }

    [Test]
    public async Task FindSessionAsync_WhenSessionDoesNotExist_ReturnsNull()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new SessionRepository(context, _timeProvider);

        // Act
        var result = await testee.FindSessionAsync(new ShoppingSessionId(999));

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task CreateSessionAsync_PersistsSessionAndReturnsItsId()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var testee = new SessionRepository(context, _timeProvider);
        var session = new ShoppingSessionBuilder().WithDefaults().Build();

        // Act
        var result = await testee.CreateSessionAsync(session);

        // Assert
        result.Should().Be(session.ShoppingSessionId);
        context.ShoppingSessions.Should().ContainSingle(saved => saved.ShoppingSessionId == session.ShoppingSessionId);
    }

    [Test]
    public async Task AddItemToSessionAsync_PersistsItemAndReturnsItsId()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var session = context.ShoppingSessions.Add(new ShoppingSessionBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();
        var testee = new SessionRepository(context, _timeProvider);
        var item = new ShoppingSessionItem("Milch", session.ShoppingSessionId, _timeProvider.GetUtcNow());

        // Act
        var result = await testee.AddItemToSessionAsync(item);

        // Assert
        result.Should().Be(item.ShoppingSessionItemId);
        context.ShoppingSessionItems.Should().ContainSingle(saved => saved.ShoppingSessionItemId == item.ShoppingSessionItemId);
    }

    [Test]
    public async Task CompleteSessionAsync_SetsStatusToCompletedAndStampsCompletedAt()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var session = context.ShoppingSessions.Add(new ShoppingSessionBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();
        var testee = new SessionRepository(context, _timeProvider);

        // Act
        await testee.CompleteSessionAsync(session);

        // Assert
        session.Status.Should().Be(SessionStatus.Completed);
        session.CompletedAt.Should().Be(_timeProvider.GetUtcNow());
    }

    [Test]
    public async Task CompleteSessionAsync_PersistsStatusAndCompletedAt()
    {
        // Arrange — uses two EfCoreContext instances sharing the same in-memory DB name, since the
        // first context's change tracker would otherwise mask a missing SaveChangesAsync call.
        var dbName = Guid.NewGuid().ToString();
        int sessionId;
        await using (var writeContext = new EfCoreContext(CreateSharedDbOptions(dbName)))
        {
            var session = writeContext.ShoppingSessions.Add(new ShoppingSessionBuilder().WithDefaults().Build()).Entity;
            await writeContext.SaveChangesAsync();
            sessionId = session.ShoppingSessionId.Value;
            var testee = new SessionRepository(writeContext, _timeProvider);

            // Act
            await testee.CompleteSessionAsync(session);
        }

        // Assert
        await using var readContext = new EfCoreContext(CreateSharedDbOptions(dbName));
        readContext.ShoppingSessions.Should().Contain(session =>
            session.ShoppingSessionId.Value == sessionId && session.Status == SessionStatus.Completed && session.CompletedAt == _timeProvider.GetUtcNow());
    }

    [Test]
    public async Task DeleteSessionAsync_RemovesSession()
    {
        // Arrange
        await using var context = new InMemoryDbContext();
        var session = context.ShoppingSessions.Add(new ShoppingSessionBuilder().WithDefaults().Build()).Entity;
        await context.SaveChangesAsync();
        var testee = new SessionRepository(context, _timeProvider);

        // Act
        await testee.DeleteSessionAsync(session);

        // Assert
        context.ShoppingSessions.Should().NotContain(session);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
