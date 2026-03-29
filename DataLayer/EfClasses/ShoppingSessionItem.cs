namespace DataLayer.EfClasses;

/// <summary>
/// Represents a single item added to the cart during a shopping session.
/// </summary>
public class ShoppingSessionItem
{
    public ShoppingSessionItem(string originalIngredient, ShoppingSessionId sessionId, DateTimeOffset addedAt)
    {
        OriginalIngredient = originalIngredient;
        SessionId = sessionId;
        AddedAt = addedAt;
    }

#pragma warning disable SA1202
    protected ShoppingSessionItem() { }
#pragma warning restore SA1202

    public ShoppingSessionItemId ShoppingSessionItemId { get; init; }

    public ShoppingSessionId SessionId { get; private set; }

    public virtual ShoppingSession ShoppingSession { get; set; }

    /// <summary>The original ingredient from the list, e.g. "500g carrots".</summary>
    public string OriginalIngredient { get; private set; } = string.Empty;

    /// <summary>The product name selected on coop.ch.</summary>
    public string SelectedProductName { get; set; } = string.Empty;

    /// <summary>The URL of the selected product on coop.ch.</summary>
    public string SelectedProductUrl { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public string Price { get; set; } = string.Empty;

    public SessionItemStatus Status { get; set; } = SessionItemStatus.Added;

    public DateTimeOffset AddedAt { get; private set; }
}
