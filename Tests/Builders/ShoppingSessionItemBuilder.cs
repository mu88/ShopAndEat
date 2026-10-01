using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>
/// Test-data builder for <see cref="ShoppingSessionItem"/>. Use <c>new ShoppingSessionItemBuilder().WithDefaults().Build()</c>.
/// All fields are plain constructor parameters (there is no post-construction update use case for this entity),
/// so a one-off value that <see cref="WithDefaults"/> doesn't cover is constructed directly via the constructor
/// rather than adding single-property builder methods here.
/// </summary>
public class ShoppingSessionItemBuilder
{
    private string _originalIngredient = string.Empty;
    private ShoppingSessionId _sessionId;
    private DateTimeOffset _addedAt;
    private string _selectedProductName = string.Empty;
    private string _selectedProductUrl = string.Empty;
    private int _quantity;
    private string _price = string.Empty;
    private SessionItemStatus _status;

    public ShoppingSessionItemBuilder WithDefaults()
    {
        _originalIngredient = "500g carrots";
        _sessionId = new ShoppingSessionId(1);
        _addedAt = new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero);
        _selectedProductName = string.Empty;
        _selectedProductUrl = string.Empty;
        _quantity = 1;
        _price = string.Empty;
        _status = SessionItemStatus.Added;
        return this;
    }

    public ShoppingSessionItem Build() =>
        new(_originalIngredient, _sessionId, _addedAt, _selectedProductName, _selectedProductUrl, _quantity, _price, _status);
}
