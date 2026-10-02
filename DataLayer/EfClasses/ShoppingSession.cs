namespace DataLayer.EfClasses;

/// <summary>
/// Represents a shopping session where the agent processed an ingredient list.
/// </summary>
public class ShoppingSession
{
    public ShoppingSession(string ingredientList, DateTimeOffset startedAt)
    {
        IngredientList = ingredientList;
        StartedAt = startedAt;
    }

#pragma warning disable SA1202
    protected ShoppingSession() { }
#pragma warning restore SA1202

    public ShoppingSessionId ShoppingSessionId { get; init; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public SessionStatus Status { get; private set; } = SessionStatus.InProgress;

    /// <summary>The original ingredient list provided by the user.</summary>
    public string IngredientList { get; private set; } = string.Empty;

    public virtual ICollection<ShoppingSessionItem> Items { get; set; } = [];

    /// <summary>Marks this session as completed, stamping <see cref="CompletedAt"/> atomically with <see cref="Status"/> so they can never drift out of sync.</summary>
    public void Complete(DateTimeOffset completedAt)
    {
        Status = SessionStatus.Completed;
        CompletedAt = completedAt;
    }
}
