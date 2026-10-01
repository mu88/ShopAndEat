namespace DataLayer.EfClasses;

public class OnlineArticleMapping
{
    public OnlineArticleMapping(string articleName, string storeKey, string storeProductCode, DateTimeOffset createdAt)
    {
        ArticleName = articleName;
        StoreKey = storeKey;
        StoreProductCode = storeProductCode;
        CreatedAt = createdAt;
    }

#pragma warning disable SA1202
    protected OnlineArticleMapping() { }
#pragma warning restore SA1202

    public OnlineArticleMappingId OnlineArticleMappingId { get; init; }

    public string ArticleName { get; private set; } = string.Empty;

    public string StoreKey { get; private set; } = string.Empty;

    public string StoreProductCode { get; private set; } = string.Empty;

    public string StoreProductName { get; private set; } = string.Empty;

    public decimal StoreProductPrice { get; private set; }

    public float Confidence { get; private set; } // 0-100

    public MatchMethod MatchMethod { get; private set; }

    /// <summary>How many ingredient units are in one store packaging unit (e.g. 6 for "6 tomatoes per pack").</summary>
    public int? QuantityPerUnit { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastUsedAt { get; private set; }

    public int FeedbackCount { get; private set; }

    /// <summary>Records a (re-)match against a store product, updating all related fields together so they never drift out of sync.</summary>
    public void RecordMatch(string storeProductName, decimal storeProductPrice, float confidence, MatchMethod matchMethod, int? quantityPerUnit, DateTimeOffset matchedAt)
    {
        StoreProductName = storeProductName;
        StoreProductPrice = storeProductPrice;
        Confidence = confidence;
        MatchMethod = matchMethod;
        QuantityPerUnit = quantityPerUnit;
        LastUsedAt = matchedAt;
    }

    /// <summary>Records user feedback on this mapping by monotonically increasing <see cref="FeedbackCount"/>.</summary>
    /// <param name="increment">The amount to increase <see cref="FeedbackCount"/> by. Must be positive.</param>
    public void RecordFeedback(int increment = 1)
    {
        if (increment < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(increment), increment, "Feedback increment must be positive.");
        }

        FeedbackCount += increment;
    }
}
