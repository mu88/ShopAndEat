namespace DataLayer.EfClasses;

/// <summary>
/// Stores learned shopping preferences, shared across all household members.
/// Preferences are injected into the LLM system prompt for context-aware shopping.
/// </summary>
public class ShoppingPreference
{
    public ShoppingPreference(string scope, string key, PreferenceSource source, string storeKey)
    {
        Scope = scope;
        Key = key;
        Source = source;
        StoreKey = storeKey;
    }

#pragma warning disable SA1202
    protected ShoppingPreference() { }
#pragma warning restore SA1202

    public ShoppingPreferenceId ShoppingPreferenceId { get; init; }

    /// <summary>'global', 'article:Tofu', 'article:Saure Sahne', etc.</summary>
    public string Scope { get; private set; }

    /// <summary>'prefer_bio', 'confirmed_product', 'avoid_product', 'search_term', etc.</summary>
    public string Key { get; private set; }

    /// <summary>JSON-encoded value, e.g. '{"brand":"Naturaplan"}' or '"Karma Bio Tofu Nature 260g"'.</summary>
    public string Value { get; set; }

    public PreferenceSource Source { get; private set; }

    /// <summary>Store identifier (e.g. "coop", "migros"). Null means shop-overarching (e.g. reminders).</summary>
    public string StoreKey { get; private set; }

    public int UsageCount { get; set; }
}
