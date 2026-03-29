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

    public string StoreProductName { get; set; } = string.Empty;

    public decimal StoreProductPrice { get; set; }

    public float Confidence { get; set; } // 0-100

    public MatchMethod MatchMethod { get; set; }

    /// <summary>How many ingredient units are in one store packaging unit (e.g. 6 for "6 tomatoes per pack").</summary>
    public int? QuantityPerUnit { get; set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastUsedAt { get; set; }

    public int FeedbackCount { get; set; }
}
