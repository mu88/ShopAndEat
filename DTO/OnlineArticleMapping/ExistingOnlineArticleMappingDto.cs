using System.Text.Json.Serialization;
using DataLayer.EfClasses;

namespace DTO.OnlineArticleMapping;

public record ExistingOnlineArticleMappingDto
{
    public int OnlineArticleMappingId { get; init; }

    public string ArticleName { get; init; } = string.Empty;

    public string StoreKey { get; init; } = string.Empty;

    public string? StoreProductCode { get; init; }

    public string? StoreProductName { get; init; }

    public decimal StoreProductPrice { get; init; }

    public float Confidence { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<MatchMethod>))]
    public MatchMethod? MatchMethod { get; init; }

    public int? QuantityPerUnit { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset LastUsedAt { get; init; }

    public int FeedbackCount { get; init; }
}
