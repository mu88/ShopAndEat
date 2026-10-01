using BizDbAccess;
using DataLayer.EfClasses;
using DTO.ShoppingSession;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using ShopAndEat.Api;
using Tests.Builders;

namespace Tests.Unit.ShopAndEat.Api;

[TestFixture]
[Category("Unit")]
public class SessionsControllerTests
{
    private ISessionRepository _sessionRepositoryMock = null!;
    private TimeProvider _timeProvider = null!;
    private SessionsController _testee = null!;

    [SetUp]
    public void SetUp()
    {
        _sessionRepositoryMock = Substitute.For<ISessionRepository>();
        _timeProvider = new FixedTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _testee = new SessionsController(_sessionRepositoryMock, _timeProvider, NullLogger<SessionsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Test]
    public async Task GetAll_ReturnsMappedSessions()
    {
        // Arrange
        var session = new ShoppingSession("Milch\nEier", _timeProvider.GetUtcNow());
        _sessionRepositoryMock.GetAllSessionsAsync(20, _testee.HttpContext.RequestAborted).Returns([session]);

        // Act
        var result = await _testee.GetAll(cancellationToken: _testee.HttpContext.RequestAborted);

        // Assert
        result.Value.Should().ContainSingle(response => response.IngredientList == "Milch\nEier");
    }

    [Test]
    public async Task GetAll_PassesLimitToRepository()
    {
        // Arrange
        _sessionRepositoryMock.GetAllSessionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _testee.GetAll(5, _testee.HttpContext.RequestAborted);

        // Assert
        await _sessionRepositoryMock.Received(1).GetAllSessionsAsync(5, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetById_WhenSessionExists_ReturnsDetailDto()
    {
        // Arrange
        var session = new ShoppingSession("Milch", _timeProvider.GetUtcNow());
        _sessionRepositoryMock.GetSessionByIdAsync(new ShoppingSessionId(7), _testee.HttpContext.RequestAborted).Returns(session);

        // Act
        var result = await _testee.GetById(7, _testee.HttpContext.RequestAborted);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<SessionDetailResponse>>()
            .Which.Value!.IngredientList.Should().Be("Milch");
    }

    [Test]
    public async Task GetById_WhenSessionDoesNotExist_ReturnsNotFoundProblem()
    {
        // Arrange
        _sessionRepositoryMock.GetSessionByIdAsync(Arg.Any<ShoppingSessionId>(), Arg.Any<CancellationToken>()).Returns((ShoppingSession?)null);

        // Act
        var result = await _testee.GetById(7, _testee.HttpContext.RequestAborted);

        // Assert
        var problem = result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>().Subject;
        problem.ProblemDetails.Status.Should().Be(404);
        problem.ProblemDetails.Detail.Should().Be("Session with the specified ID was not found.");
    }

    [Test]
    public async Task Create_CreatesSessionWithCurrentTime_AndReturnsCreatedResponse()
    {
        // Arrange
        _sessionRepositoryMock.CreateSessionAsync(Arg.Any<ShoppingSession>(), Arg.Any<CancellationToken>()).Returns(new ShoppingSessionId(42));
        var request = new CreateSessionRequest { IngredientList = "Milch" };

        // Act
        var result = await _testee.Create(request, _testee.HttpContext.RequestAborted);

        // Assert
        result.Value!.ShoppingSessionId.Should().Be(42);
        result.Location.Should().Be("/api/sessions/42");
        await _sessionRepositoryMock.Received(1).CreateSessionAsync(
            Arg.Is<ShoppingSession>(session => session.IngredientList == "Milch" && session.StartedAt == _timeProvider.GetUtcNow()),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddItem_WhenSessionDoesNotExist_ReturnsNotFoundProblem()
    {
        // Arrange
        _sessionRepositoryMock.FindSessionAsync(Arg.Any<ShoppingSessionId>(), Arg.Any<CancellationToken>()).Returns((ShoppingSession?)null);
        var request = new AddSessionItemRequest { OriginalIngredient = "Milch", SelectedProductName = "Bio Milch", SelectedProductUrl = "https://example.test/milch" };

        // Act
        var result = await _testee.AddItem(1, request, _testee.HttpContext.RequestAborted);

        // Assert
        var problem = result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>().Subject;
        problem.ProblemDetails.Status.Should().Be(404);
        problem.ProblemDetails.Detail.Should().Be("Session with the specified ID was not found.");
    }

    [Test]
    public async Task AddItem_WhenSessionIsNotInProgress_ReturnsBadRequestProblem()
    {
        // Arrange
        var session = new ShoppingSessionBuilder().WithDefaults().AsCompleted(_timeProvider.GetUtcNow()).Build();
        _sessionRepositoryMock.FindSessionAsync(Arg.Any<ShoppingSessionId>(), Arg.Any<CancellationToken>()).Returns(session);
        var request = new AddSessionItemRequest { OriginalIngredient = "Milch", SelectedProductName = "Bio Milch", SelectedProductUrl = "https://example.test/milch" };

        // Act
        var result = await _testee.AddItem(1, request, _testee.HttpContext.RequestAborted);

        // Assert
        var problem = result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>().Subject;
        problem.ProblemDetails.Status.Should().Be(400);
        problem.ProblemDetails.Detail.Should().Be("Cannot add items to a session that is not in progress.");
        problem.ProblemDetails.Title.Should().Be("Invalid Operation");
    }

    [Test]
    public async Task AddItem_WhenSessionInProgress_AddsItemAndReturnsOk()
    {
        // Arrange
        var session = new ShoppingSessionBuilder().WithDefaults().Build();
        _sessionRepositoryMock.FindSessionAsync(new ShoppingSessionId(1), _testee.HttpContext.RequestAborted).Returns(session);
        _sessionRepositoryMock.AddItemToSessionAsync(Arg.Any<ShoppingSessionItem>(), Arg.Any<CancellationToken>()).Returns(new ShoppingSessionItemId(9));
        var request = new AddSessionItemRequest
        {
            OriginalIngredient = "Milch",
            SelectedProductName = "Bio Milch",
            SelectedProductUrl = "https://example.test/milch",
            Quantity = 2,
            Price = "CHF 2.40",
            Status = SessionItemStatus.Added,
        };

        // Act
        var result = await _testee.AddItem(1, request, _testee.HttpContext.RequestAborted);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<CreateSessionItemResponse>>()
            .Which.Value!.ShoppingSessionItemId.Should().Be(9);
        await _sessionRepositoryMock.Received(1).AddItemToSessionAsync(
            Arg.Is<ShoppingSessionItem>(item =>
                item.OriginalIngredient == "Milch" &&
                item.SelectedProductName == "Bio Milch" &&
                item.SelectedProductUrl == "https://example.test/milch" &&
                item.Quantity == 2 &&
                item.Price == "CHF 2.40" &&
                item.Status == SessionItemStatus.Added),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Delete_WhenSessionExists_DeletesAndReturnsNoContent()
    {
        // Arrange
        var session = new ShoppingSessionBuilder().WithDefaults().Build();
        _sessionRepositoryMock.FindSessionAsync(new ShoppingSessionId(1), _testee.HttpContext.RequestAborted).Returns(session);

        // Act
        var result = await _testee.Delete(1, _testee.HttpContext.RequestAborted);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        await _sessionRepositoryMock.Received(1).DeleteSessionAsync(session, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Delete_WhenSessionDoesNotExist_ReturnsNotFoundProblem()
    {
        // Arrange
        _sessionRepositoryMock.FindSessionAsync(Arg.Any<ShoppingSessionId>(), Arg.Any<CancellationToken>()).Returns((ShoppingSession?)null);

        // Act
        var result = await _testee.Delete(1, _testee.HttpContext.RequestAborted);

        // Assert
        var problem = result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>().Subject;
        problem.ProblemDetails.Status.Should().Be(404);
        problem.ProblemDetails.Detail.Should().Be("Session with the specified ID was not found.");
    }

    [Test]
    public async Task Complete_WhenSessionExists_CompletesAndReturnsNoContent()
    {
        // Arrange
        var session = new ShoppingSessionBuilder().WithDefaults().Build();
        _sessionRepositoryMock.FindSessionAsync(new ShoppingSessionId(1), _testee.HttpContext.RequestAborted).Returns(session);

        // Act
        var result = await _testee.Complete(1, _testee.HttpContext.RequestAborted);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
        await _sessionRepositoryMock.Received(1).CompleteSessionAsync(session, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Complete_WhenSessionDoesNotExist_ReturnsNotFoundProblem()
    {
        // Arrange
        _sessionRepositoryMock.FindSessionAsync(Arg.Any<ShoppingSessionId>(), Arg.Any<CancellationToken>()).Returns((ShoppingSession?)null);

        // Act
        var result = await _testee.Complete(1, _testee.HttpContext.RequestAborted);

        // Assert
        var problem = result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>().Subject;
        problem.ProblemDetails.Status.Should().Be(404);
        problem.ProblemDetails.Detail.Should().Be("Session with the specified ID was not found.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
