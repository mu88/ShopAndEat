namespace DataLayer.EfClasses;

/// <summary>
/// Represents a single item added to the cart during a shopping session.
/// </summary>
public class ShoppingSessionItem
{
    // S107: the 8-parameter constructor mirrors ShoppingSessionItem's DB-mapped columns 1:1 (it has
    // no sub-groupable concepts), and is used across 2 production call sites (SessionsController.AddItem,
    // ServerSessionAdapter.AddSessionItemAsync); introducing a parameter object here would be a larger
    // design change than this analyzer cleanup warrants.
#pragma warning disable S107
    public ShoppingSessionItem(
        string originalIngredient,
        ShoppingSessionId sessionId,
        DateTimeOffset addedAt,
        string selectedProductName = "",
        string selectedProductUrl = "",
        int quantity = 1,
        string price = "",
        SessionItemStatus status = SessionItemStatus.Added)
#pragma warning restore S107
    {
        OriginalIngredient = originalIngredient;
        SessionId = sessionId;
        AddedAt = addedAt;
        SelectedProductName = selectedProductName;
        SelectedProductUrl = selectedProductUrl;
        Quantity = quantity;
        Price = price;
        Status = status;
    }

#pragma warning disable SA1202
    protected ShoppingSessionItem() { }
#pragma warning restore SA1202

    public ShoppingSessionItemId ShoppingSessionItemId { get; init; }

    public ShoppingSessionId SessionId { get; private set; }

    public virtual ShoppingSession ShoppingSession { get; set; } = null!;

    /// <summary>The original ingredient from the list, e.g. "500g carrots".</summary>
    public string OriginalIngredient { get; private set; } = string.Empty;

    /// <summary>The product name selected on coop.ch.</summary>
    public string SelectedProductName { get; private set; } = string.Empty;

    /// <summary>The URL of the selected product on coop.ch.</summary>
    public string SelectedProductUrl { get; private set; } = string.Empty;

    public int Quantity { get; private set; } = 1;

    public string Price { get; private set; } = string.Empty;

    public SessionItemStatus Status { get; private set; } = SessionItemStatus.Added;

    public DateTimeOffset AddedAt { get; private set; }
}
