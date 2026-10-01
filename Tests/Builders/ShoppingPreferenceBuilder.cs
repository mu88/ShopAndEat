using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>
/// Test-data builder for <see cref="ShoppingPreference"/>. Use <c>new ShoppingPreferenceBuilder().WithDefaults().Build()</c>.
/// <see cref="ShoppingPreference.Value"/> and <see cref="ShoppingPreference.UsageCount"/> are plain public setters
/// (deliberately kept as a pragmatic exception, since no real invariant links them to any other field), so tests
/// needing a specific value simply assign the built instance's property directly instead of going through a builder method.
/// </summary>
public class ShoppingPreferenceBuilder
{
    private string _scope = string.Empty;
    private string _key = string.Empty;
    private PreferenceSource _source;
    private string? _storeKey;

    public ShoppingPreferenceBuilder WithDefaults()
    {
        _scope = "global";

        // Unique per instance: Scope+Key+StoreKey has a unique index, and tests may build several preferences.
        _key = $"fixture-key-{Guid.NewGuid():N}";
        _source = PreferenceSource.UserConfirmed;
        _storeKey = "coop";
        return this;
    }

    public ShoppingPreference Build() => new(_scope, _key, _source, _storeKey);
}
