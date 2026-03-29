using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DataLayer.EfClasses;

namespace DTO.OnlineArticleMapping;

public record NewOnlineArticleMappingDto
{
    [Required]
    [MaxLength(500)]
    public string ArticleName { get; init; } = string.Empty;

    public string? StoreKey { get; init; }

    [MaxLength(100)]
    public string? StoreProductCode { get; init; }

    [MaxLength(500)]
    public string? StoreProductName { get; init; }

    public decimal StoreProductPrice { get; init; }

    [Range(0, 100)]
    public float Confidence { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<MatchMethod>))]
    public MatchMethod? MatchMethod { get; init; }

    public int? QuantityPerUnit { get; init; }
}
