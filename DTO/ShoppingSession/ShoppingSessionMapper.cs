namespace DTO.ShoppingSession;

public static class ShoppingSessionMapper
{
    public static SessionResponse ToDto(this DataLayer.EfClasses.ShoppingSession session) => new()
    {
        SessionId = session.ShoppingSessionId.Value,
        StartedAt = session.StartedAt,
        CompletedAt = session.CompletedAt,
        Status = session.Status.ToString(),
        IngredientList = session.IngredientList,
        ItemCount = session.Items.Count,
    };

    public static SessionItemResponse ToDto(this DataLayer.EfClasses.ShoppingSessionItem item) => new()
    {
        OriginalIngredient = item.OriginalIngredient,
        SelectedProductName = item.SelectedProductName,
        SelectedProductUrl = item.SelectedProductUrl,
        Quantity = item.Quantity,
        Price = item.Price,
        Status = item.Status.ToString(),
        AddedAt = item.AddedAt,
    };

    public static SessionDetailResponse ToDetailDto(this DataLayer.EfClasses.ShoppingSession session) => new()
    {
        SessionId = session.ShoppingSessionId.Value,
        StartedAt = session.StartedAt,
        CompletedAt = session.CompletedAt,
        Status = session.Status.ToString(),
        IngredientList = session.IngredientList,
        Items = session.Items.Select(item => item.ToDto()).ToArray(),
    };
}
