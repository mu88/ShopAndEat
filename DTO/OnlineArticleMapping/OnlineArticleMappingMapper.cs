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
    {
        var mapping = new EfOnlineArticleMapping(dto.ArticleName, storeKey, dto.StoreProductCode ?? string.Empty, default);
        mapping.RecordMatch(dto.StoreProductName ?? string.Empty, dto.StoreProductPrice, dto.Confidence, dto.MatchMethod ?? default, dto.QuantityPerUnit, default);
        return mapping;
    }
}
