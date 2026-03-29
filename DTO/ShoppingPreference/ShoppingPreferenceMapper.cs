namespace DTO.ShoppingPreference;

public static class ShoppingPreferenceMapper
{
    public static PreferenceResponse ToPreferenceResponse(this DataLayer.EfClasses.ShoppingPreference preference) => new()
    {
        Scope = preference.Scope,
        Key = preference.Key,
        Value = preference.Value,
        Source = preference.Source.ToString(),
        StoreKey = preference.StoreKey,
        UsageCount = preference.UsageCount,
    };
}
