using AutoMapper;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Meal;
using DTO.MealType;
using DTO.PurchaseItem;
using DTO.Recipe;
using DTO.Store;
using DTO.Unit;

namespace DTO;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<NewArticleGroupDto, DataLayer.EfClasses.ArticleGroup>().IgnoreAllPropertiesWithAnInaccessibleSetter();
        CreateMap<NewMealTypeDto, DataLayer.EfClasses.MealType>().IgnoreAllPropertiesWithAnInaccessibleSetter();
        CreateMap<NewUnitDto, DataLayer.EfClasses.Unit>().IgnoreAllPropertiesWithAnInaccessibleSetter();
        CreateMap<NewIngredientDto, DataLayer.EfClasses.Ingredient>().IgnoreAllPropertiesWithAnInaccessibleSetter();
        CreateMap<NewArticleDto, DataLayer.EfClasses.Article>().IgnoreAllPropertiesWithAnInaccessibleSetter();
        CreateMap<NewPurchaseItemDto, DataLayer.EfClasses.PurchaseItem>().IgnoreAllPropertiesWithAnInaccessibleSetter();

        CreateMap<ExistingArticleGroupDto, DataLayer.EfClasses.ArticleGroup>()
            .IgnoreAllPropertiesWithAnInaccessibleSetter()
            .ReverseMap();
        CreateMap<ExistingMealTypeDto, DataLayer.EfClasses.MealType>().IgnoreAllPropertiesWithAnInaccessibleSetter().ReverseMap();
        CreateMap<ExistingUnitDto, DataLayer.EfClasses.Unit>().IgnoreAllPropertiesWithAnInaccessibleSetter().ReverseMap();
        CreateMap<ExistingArticleDto, DataLayer.EfClasses.Article>().IgnoreAllPropertiesWithAnInaccessibleSetter().ReverseMap();
        CreateMap<ExistingIngredientDto, DataLayer.EfClasses.Ingredient>().IgnoreAllPropertiesWithAnInaccessibleSetter().ReverseMap();
        CreateMap<ExistingPurchaseItemDto, DataLayer.EfClasses.PurchaseItem>()
            .IgnoreAllPropertiesWithAnInaccessibleSetter()
            .ReverseMap();
        CreateMap<NewPurchaseItemDto, DataLayer.EfClasses.PurchaseItem>().IgnoreAllPropertiesWithAnInaccessibleSetter().ReverseMap();
        CreateMap<ExistingMealDto, DataLayer.EfClasses.Meal>().IgnoreAllPropertiesWithAnInaccessibleSetter().ReverseMap();
        CreateMap<ExistingRecipeDto, DataLayer.EfClasses.Recipe>().IgnoreAllPropertiesWithAnInaccessibleSetter().ReverseMap();
        CreateMap<ExistingStoreDto, DataLayer.EfClasses.Store>().IgnoreAllPropertiesWithAnInaccessibleSetter().ReverseMap();
    }
}
