using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BizDbAccess;
using DataLayer.EfClasses;
using DTO.ShoppingSession;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ShopAndEat.Logging;

namespace ShopAndEat.Api;

[ApiController]
[Route("api/shopping/sessions")]
public class SessionsController(ISessionRepository sessionRepository, TimeProvider timeProvider, ILogger<SessionsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<Ok<IReadOnlyList<SessionResponse>>> GetAll([FromQuery][Range(1, 1000)] int limit = 20, CancellationToken cancellationToken = default)
    {
        var sessions = await sessionRepository.GetAllSessionsAsync(limit, cancellationToken);

        return TypedResults.Ok<IReadOnlyList<SessionResponse>>(sessions.Select(session => session.ToDto()).ToArray());
    }

    [HttpGet("{id}")]
    public async Task<Results<Ok<SessionDetailResponse>, ProblemHttpResult>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.GetSessionByIdAsync(new ShoppingSessionId(id), cancellationToken);

        if (session == null)
        {
            return TypedResults.Problem(detail: "Session with the specified ID was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(session.ToDetailDto());
    }

    [HttpPost]
    public async Task<Created<CreateSessionResponse>> Create([FromBody] CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = new ShoppingSession(request.IngredientList, timeProvider.GetUtcNow());

        var sessionId = await sessionRepository.CreateSessionAsync(session, cancellationToken);
        ControllerLogMessages.SessionCreated(logger, sessionId.Value);

        return TypedResults.Created($"/api/sessions/{sessionId.Value.ToString(CultureInfo.InvariantCulture)}", new CreateSessionResponse { ShoppingSessionId = sessionId.Value });
    }

    [HttpPost("{id}/items")]
    public async Task<Results<Ok<CreateSessionItemResponse>, ProblemHttpResult>> AddItem(int id, [FromBody] AddSessionItemRequest request, CancellationToken cancellationToken = default)
    {
        var typedId = new ShoppingSessionId(id);
        var session = await sessionRepository.FindSessionAsync(typedId, cancellationToken);
        if (session == null)
        {
            return TypedResults.Problem(detail: "Session with the specified ID was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        if (session.Status != SessionStatus.InProgress)
        {
            return TypedResults.Problem(detail: "Cannot add items to a session that is not in progress.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid Operation");
        }

        var item = new ShoppingSessionItem(request.OriginalIngredient, typedId, timeProvider.GetUtcNow())
        {
            SelectedProductName = request.SelectedProductName,
            SelectedProductUrl = request.SelectedProductUrl,
            Quantity = request.Quantity,
            Price = request.Price,
            Status = request.Status,
        };

        var itemId = await sessionRepository.AddItemToSessionAsync(item, cancellationToken);

        return TypedResults.Ok(new CreateSessionItemResponse { ShoppingSessionItemId = itemId.Value });
    }

    [HttpDelete("{id}")]
    public async Task<Results<NoContent, ProblemHttpResult>> Delete(int id, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.FindSessionAsync(new ShoppingSessionId(id), cancellationToken);
        if (session == null)
        {
            return TypedResults.Problem(detail: "Session with the specified ID was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        await sessionRepository.DeleteSessionAsync(session, cancellationToken);
        ControllerLogMessages.SessionDeleted(logger, id);

        return TypedResults.NoContent();
    }

    [HttpPatch("{id}/complete")]
    public async Task<Results<NoContent, ProblemHttpResult>> Complete(int id, CancellationToken cancellationToken = default)
    {
        var session = await sessionRepository.FindSessionAsync(new ShoppingSessionId(id), cancellationToken);
        if (session == null)
        {
            return TypedResults.Problem(detail: "Session with the specified ID was not found.", statusCode: StatusCodes.Status404NotFound);
        }

        await sessionRepository.CompleteSessionAsync(session, cancellationToken);
        ControllerLogMessages.SessionCompleted(logger, id);

        return TypedResults.NoContent();
    }
}
