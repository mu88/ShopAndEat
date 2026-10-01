using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="ShoppingSession"/>. Use <c>new ShoppingSessionBuilder().WithDefaults().Build()</c>.</summary>
public class ShoppingSessionBuilder
{
    private readonly List<Action<ShoppingSession>> _postBuildActions = [];
    private string _ingredientList = string.Empty;
    private DateTimeOffset _startedAt;

    public ShoppingSessionBuilder WithDefaults()
    {
        _ingredientList = "500g carrots\n1L milk";
        _startedAt = new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero);
        _postBuildActions.Clear();
        return this;
    }

    /// <summary>Builds a session that has already been completed, using the real <see cref="ShoppingSession.Complete"/> business method so <c>Status</c>/<c>CompletedAt</c> can never drift out of sync.</summary>
    public ShoppingSessionBuilder AsCompleted(DateTimeOffset completedAt)
    {
        _postBuildActions.Add(session => session.Complete(completedAt));
        return this;
    }

    public ShoppingSession Build()
    {
        var session = new ShoppingSession(_ingredientList, _startedAt);
        foreach (var postBuildAction in _postBuildActions)
        {
            postBuildAction(session);
        }

        return session;
    }
}
