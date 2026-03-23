using DTO.Article;
using DTO.Unit;
using EfIngredient = DataLayer.EfClasses.Ingredient;

namespace DTO.Ingredient;

public static class IngredientMapper
{
    public static ExistingIngredientDto ToDto(this EfIngredient entity)
        => new(entity.Article.ToDto(), entity.Quantity, entity.Unit.ToDto(), entity.IngredientId);

    public static EfIngredient ToEntity(this NewIngredientDto dto)
        => new(dto.Article.ToEntity(), dto.Quantity, dto.Unit.ToEntity());

    public static EfIngredient ToEntity(this ExistingIngredientDto dto)
        => new(dto.Article.ToEntity(), dto.Quantity, dto.Unit.ToEntity());
}
