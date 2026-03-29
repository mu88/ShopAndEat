using EfOnlineArticleMapping = DataLayer.EfClasses.OnlineArticleMapping;

namespace DTO.OnlineArticleMapping;

public static class OnlineArticleMappingMapper
{
    public static ExistingOnlineArticleMappingDto ToDto(this EfOnlineArticleMapping entity)
        => new()
        {
            OnlineArticleMappingId = entity.OnlineArticleMappingId.Value,
            ArticleName = entity.ArticleName,
            StoreKey = entity.StoreKey,
            StoreProductCode = entity.StoreProductCode,
            StoreProductName = entity.StoreProductName,
            StoreProductPrice = entity.StoreProductPrice,
            Confidence = entity.Confidence,
            MatchMethod = entity.MatchMethod,
            QuantityPerUnit = entity.QuantityPerUnit,
            CreatedAt = entity.CreatedAt,
            LastUsedAt = entity.LastUsedAt,
            FeedbackCount = entity.FeedbackCount,
        };

    public static EfOnlineArticleMapping ToEntity(this NewOnlineArticleMappingDto dto, string storeKey)
        => new(dto.ArticleName, storeKey, dto.StoreProductCode, default)
        {
            StoreProductName = dto.StoreProductName,
            StoreProductPrice = dto.StoreProductPrice,
            Confidence = dto.Confidence,
            MatchMethod = dto.MatchMethod ?? default,
            QuantityPerUnit = dto.QuantityPerUnit,
        };
}
