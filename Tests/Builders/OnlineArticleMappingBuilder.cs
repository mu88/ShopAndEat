using DataLayer.EfClasses;

namespace Tests.Builders;

/// <summary>Test-data builder for <see cref="OnlineArticleMapping"/>. Use <c>new OnlineArticleMappingBuilder().WithDefaults().Build()</c>.</summary>
public class OnlineArticleMappingBuilder
{
    private readonly List<Action<OnlineArticleMapping>> _postBuildActions = [];
    private string _articleName = string.Empty;
    private string _storeKey = string.Empty;
    private string _storeProductCode = string.Empty;
    private DateTimeOffset _createdAt;

    public OnlineArticleMappingBuilder WithDefaults()
    {
        _articleName = "Tomato";
        _storeKey = "coop";

        // Unique per instance: ArticleName+StoreKey+StoreProductCode has a unique index, and tests may build several mappings.
        _storeProductCode = $"fixture-product-{Guid.NewGuid():N}";
        _createdAt = new DateTimeOffset(2024, 1, 15, 8, 0, 0, TimeSpan.Zero);
        _postBuildActions.Clear();
        return this;
    }

    /// <summary>Records a match via the real <see cref="OnlineArticleMapping.RecordMatch"/> business method, so all match-related fields stay consistent.</summary>
    public OnlineArticleMappingBuilder WithMatch(string storeProductName, decimal storeProductPrice, float confidence, MatchMethod matchMethod, int? quantityPerUnit, DateTimeOffset matchedAt)
    {
        _postBuildActions.Add(mapping => mapping.RecordMatch(storeProductName, storeProductPrice, confidence, matchMethod, quantityPerUnit, matchedAt));
        return this;
    }

    /// <summary>Records feedback via the real <see cref="OnlineArticleMapping.RecordFeedback"/> business method, so <c>FeedbackCount</c> can only ever increase monotonically.</summary>
    public OnlineArticleMappingBuilder WithFeedback(int increment = 1)
    {
        _postBuildActions.Add(mapping => mapping.RecordFeedback(increment));
        return this;
    }

    public OnlineArticleMapping Build()
    {
        var mapping = new OnlineArticleMapping(_articleName, _storeKey, _storeProductCode, _createdAt);
        foreach (var postBuildAction in _postBuildActions)
        {
            postBuildAction(mapping);
        }

        return mapping;
    }
}
