using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Meal;
using DTO.MealType;
using DTO.OnlineArticleMapping;
using DTO.PurchaseItem;
using DTO.Recipe;
using DTO.ShoppingPreference;
using DTO.ShoppingSession;
using DTO.Store;
using DTO.Unit;
using FluentAssertions;
using NUnit.Framework;
using EfArticle = DataLayer.EfClasses.Article;
using EfArticleGroup = DataLayer.EfClasses.ArticleGroup;
using EfIngredient = DataLayer.EfClasses.Ingredient;
using EfMeal = DataLayer.EfClasses.Meal;
using EfMealType = DataLayer.EfClasses.MealType;
using EfPurchaseItem = DataLayer.EfClasses.PurchaseItem;
using EfRecipe = DataLayer.EfClasses.Recipe;
using EfShoppingOrder = DataLayer.EfClasses.ShoppingOrder;
using EfShoppingSession = DataLayer.EfClasses.ShoppingSession;
using EfShoppingSessionItem = DataLayer.EfClasses.ShoppingSessionItem;
using EfStore = DataLayer.EfClasses.Store;
using EfUnit = DataLayer.EfClasses.Unit;

namespace Tests.Unit.DTO;

[TestFixture]
[Category("Unit")]
public class MapperTests
{
    [Test]
    public void ArticleGroupMapper_ToDto_MapsAllProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var entity = context.ArticleGroups.Add(new EfArticleGroup("Vegetables")).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.ArticleGroupId.Should().Be(entity.ArticleGroupId);
        dto.Name.Should().Be(entity.Name);
    }

    [Test]
    public void ArticleGroupMapper_ToEntity_MapsNewDto()
    {
        // Arrange
        var dto = new NewArticleGroupDto("Vegetables");

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Name.Should().Be(dto.Name);
    }

    [Test]
    public void ArticleGroupMapper_ToEntity_MapsExistingDto()
    {
        // Arrange
        var dto = new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(5), "Vegetables");

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Name.Should().Be(dto.Name);
    }

    [Test]
    public void UnitMapper_ToDto_MapsAllProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var entity = context.Units.Add(new EfUnit("Piece")).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.UnitId.Should().Be(entity.UnitId);
        dto.Name.Should().Be(entity.Name);
    }

    [Test]
    public void UnitMapper_ToEntity_MapsNewDto()
    {
        // Arrange
        var dto = new NewUnitDto("Piece");

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Name.Should().Be(dto.Name);
    }

    [Test]
    public void UnitMapper_ToEntity_MapsExistingDto()
    {
        // Arrange
        var dto = new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(6), "Piece");

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Name.Should().Be(dto.Name);
    }

    [Test]
    public void MealTypeMapper_ToDto_MapsAllProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var entity = context.MealTypes.Add(new EfMealType("Lunch", 2)).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.MealTypeId.Should().Be(entity.MealTypeId);
        dto.Name.Should().Be(entity.Name);
        dto.Order.Should().Be(entity.Order);
    }

    [Test]
    public void MealTypeMapper_ToEntity_MapsNewDto()
    {
        // Arrange
        var dto = new NewMealTypeDto("Lunch");

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Name.Should().Be(dto.Name);
    }

    [Test]
    public void MealTypeMapper_ToEntity_MapsExistingDto()
    {
        // Arrange
        var dto = new ExistingMealTypeDto("Lunch", 7, 2);

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Name.Should().Be(dto.Name);
        entity.Order.Should().Be(dto.Order);
    }

    [Test]
    public void ArticleMapper_ToDto_MapsNestedProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new EfArticleGroup("Vegetables")).Entity;
        var entity = context.Articles.Add(new EfArticle("Tomato", articleGroup, isInventory: true)).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.ArticleId.Should().Be(entity.ArticleId);
        dto.Name.Should().Be(entity.Name);
        dto.IsInventory.Should().BeTrue();
        dto.ArticleGroup.ArticleGroupId.Should().Be(articleGroup.ArticleGroupId);
        dto.ArticleGroup.Name.Should().Be(articleGroup.Name);
    }

    [Test]
    public void ArticleMapper_ToEntity_MapsNewDto()
    {
        // Arrange
        var dto = new NewArticleDto("Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(8), "Vegetables"), true);

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Name.Should().Be(dto.Name);
        entity.IsInventory.Should().Be(dto.IsInventory);
        entity.ArticleGroup.Name.Should().Be(dto.ArticleGroup.Name);
    }

    [Test]
    public void ArticleMapper_ToEntity_MapsExistingDto()
    {
        // Arrange
        var dto = new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(9), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(8), "Vegetables"), true);

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Name.Should().Be(dto.Name);
        entity.IsInventory.Should().Be(dto.IsInventory);
        entity.ArticleGroup.Name.Should().Be(dto.ArticleGroup.Name);
    }

    [Test]
    public void IngredientMapper_ToDto_MapsNestedProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new EfArticleGroup("Vegetables")).Entity;
        var article = context.Articles.Add(new EfArticle("Tomato", articleGroup, isInventory: false)).Entity;
        var unit = context.Units.Add(new EfUnit("Piece")).Entity;
        var entity = context.Ingredients.Add(new EfIngredient(article, 2.5, unit)).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.IngredientId.Should().Be(entity.IngredientId);
        dto.Quantity.Should().Be(entity.Quantity);
        dto.Article.ArticleId.Should().Be(article.ArticleId);
        dto.Article.Name.Should().Be(article.Name);
        dto.Unit.UnitId.Should().Be(unit.UnitId);
        dto.Unit.Name.Should().Be(unit.Name);
    }

    [Test]
    public void IngredientMapper_ToEntity_MapsNewDto()
    {
        // Arrange
        var dto = new NewIngredientDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(10), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(8), "Vegetables"), false),
            2.5,
            new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(11), "Piece"));

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Quantity.Should().Be(dto.Quantity);
        entity.Article.Name.Should().Be(dto.Article.Name);
        entity.Unit.Name.Should().Be(dto.Unit.Name);
    }

    [Test]
    public void IngredientMapper_ToEntity_MapsExistingDto()
    {
        // Arrange
        var dto = new ExistingIngredientDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(10), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(8), "Vegetables"), false), 2.5, new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(11), "Piece"), 12);

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Quantity.Should().Be(dto.Quantity);
        entity.Article.Name.Should().Be(dto.Article.Name);
        entity.Unit.Name.Should().Be(dto.Unit.Name);
    }

    [Test]
    public void PurchaseItemMapper_ToDto_MapsNestedProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new EfArticleGroup("Vegetables")).Entity;
        var article = context.Articles.Add(new EfArticle("Tomato", articleGroup, isInventory: false)).Entity;
        var unit = context.Units.Add(new EfUnit("Piece")).Entity;
        var entity = context.PurchaseItems.Add(new EfPurchaseItem(article, 4, unit)).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.PurchaseItemId.Should().Be(entity.PurchaseItemId);
        dto.Quantity.Should().Be((uint)entity.Quantity);
        dto.Article.ArticleId.Should().Be(article.ArticleId);
        dto.Unit.UnitId.Should().Be(unit.UnitId);
    }

    [Test]
    public void PurchaseItemMapper_ToNewDto_MapsNestedProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new EfArticleGroup("Vegetables")).Entity;
        var article = context.Articles.Add(new EfArticle("Tomato", articleGroup, isInventory: false)).Entity;
        var unit = context.Units.Add(new EfUnit("Piece")).Entity;
        var entity = context.PurchaseItems.Add(new EfPurchaseItem(article, 4, unit)).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToNewDto();

        // Assert
        dto.Quantity.Should().Be(entity.Quantity);
        dto.Article.ArticleId.Should().Be(article.ArticleId);
        dto.Unit.UnitId.Should().Be(unit.UnitId);
    }

    [Test]
    public void PurchaseItemMapper_ToEntity_MapsNewDto()
    {
        // Arrange
        var dto = new NewPurchaseItemDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(10), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(8), "Vegetables"), false),
            new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(11), "Piece"),
            4);

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Quantity.Should().Be(dto.Quantity);
        entity.Article.Name.Should().Be(dto.Article.Name);
        entity.Unit.Name.Should().Be(dto.Unit.Name);
    }

    [Test]
    public void PurchaseItemMapper_ToEntity_MapsExistingDto()
    {
        // Arrange
        var dto = new ExistingPurchaseItemDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(10), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(8), "Vegetables"), false), new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(11), "Piece"), 4, 13);

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Quantity.Should().Be(dto.Quantity);
        entity.Article.Name.Should().Be(dto.Article.Name);
        entity.Unit.Name.Should().Be(dto.Unit.Name);
    }

    [Test]
    public void NewPurchaseItemDto_ToString_UsesQuantityAndArticleName_WhenUnitIsPiece()
    {
        // Arrange
        var dto = new NewPurchaseItemDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(10), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(8), "Vegetables"), false),
            new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(11), "piece"),
            4);

        // Act
        var result = dto.ToString();

        // Assert
        result.Should().Be("4 Tomato");
    }

    [Test]
    public void NewPurchaseItemDto_ToString_IncludesUnitName_WhenUnitIsNotPiece()
    {
        // Arrange
        var dto = new NewPurchaseItemDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(10), "Milk", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(8), "Diary"), false),
            new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(11), "Liter"),
            2);

        // Act
        var result = dto.ToString();

        // Assert
        result.Should().Be("2 Liter Milk");
    }

    [Test]
    public void ShoppingPreferenceMapper_ToPreferenceResponse_MapsNonNullValue()
    {
        // Arrange
        var preference = new global::DataLayer.EfClasses.ShoppingPreference("global", "favoriteStore", global::DataLayer.EfClasses.PreferenceSource.UserConfirmed, "coop") { Value = "Coop", UsageCount = 3 };

        // Act
        var response = preference.ToPreferenceResponse();

        // Assert
        response.Value.Should().Be("Coop");
    }

    [Test]
    public void ShoppingPreferenceMapper_ToPreferenceResponse_MapsNullValueToEmptyString()
    {
        // Arrange
        var preference = new global::DataLayer.EfClasses.ShoppingPreference("global", "favoriteStore", global::DataLayer.EfClasses.PreferenceSource.UserConfirmed, storeKey: null) { Value = null };

        // Act
        var response = preference.ToPreferenceResponse();

        // Assert
        response.Value.Should().BeEmpty();
    }

    [Test]
    public void OnlineArticleMappingMapper_ToEntity_MapsOptionalFieldsWhenPresent()
    {
        // Arrange
        var dto = new NewOnlineArticleMappingDto
        {
            ArticleName = "Tomato",
            StoreProductCode = "12345",
            StoreProductName = "Fresh Tomato",
            StoreProductPrice = 2.5m,
            Confidence = 90,
            MatchMethod = MatchMethod.UserChosen,
            QuantityPerUnit = 6,
        };

        // Act
        var entity = dto.ToEntity("coop");

        // Assert
        entity.StoreProductCode.Should().Be("12345");
        entity.StoreProductName.Should().Be("Fresh Tomato");
        entity.MatchMethod.Should().Be(MatchMethod.UserChosen);
    }

    [Test]
    public void OnlineArticleMappingMapper_ToEntity_MapsDefaultsWhenOptionalFieldsAreNull()
    {
        // Arrange
        var dto = new NewOnlineArticleMappingDto
        {
            ArticleName = "Tomato",
            StoreProductCode = null,
            StoreProductName = null,
            MatchMethod = null,
        };

        // Act
        var entity = dto.ToEntity("coop");

        // Assert
        entity.StoreProductCode.Should().Be(string.Empty);
        entity.StoreProductName.Should().Be(string.Empty);
        entity.MatchMethod.Should().Be(default(MatchMethod));
    }

    [Test]
    public void RecipeMapper_ToDto_MapsNestedIngredients()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new EfArticleGroup("Vegetables")).Entity;
        var article = context.Articles.Add(new EfArticle("Tomato", articleGroup, isInventory: false)).Entity;
        var unit = context.Units.Add(new EfUnit("Piece")).Entity;
        var ingredient = context.Ingredients.Add(new EfIngredient(article, 2, unit)).Entity;
        var ingredients = new List<EfIngredient> { ingredient };
        var entity = context.Recipes.Add(new EfRecipe("Soup", 2, 4, ingredients)).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.RecipeId.Should().Be(entity.RecipeId);
        dto.Name.Should().Be(entity.Name);
        dto.NumberOfDays.Should().Be(entity.NumberOfDays);
        dto.NumberOfPersons.Should().Be(entity.NumberOfPersons);
        dto.Ingredients.Should().ContainSingle();
        dto.Ingredients.Single().IngredientId.Should().Be(ingredient.IngredientId);
    }

    [Test]
    public void MealMapper_ToDto_MapsNestedProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var mealType = context.MealTypes.Add(new EfMealType("Lunch", 2)).Entity;
        var ingredients = new List<EfIngredient>();
        var recipe = context.Recipes.Add(new EfRecipe("Soup", 2, 4, ingredients)).Entity;
        var entity = context.Meals.Add(new EfMeal(DateTime.Today, mealType, recipe, 4)).Entity;
        entity.MarkAsShopped();
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.MealId.Should().Be(entity.MealId);
        dto.Day.Should().Be(entity.Day);
        dto.HasBeenShopped.Should().BeTrue();
        dto.NumberOfPersons.Should().Be(entity.NumberOfPersons);
        dto.MealType.MealTypeId.Should().Be(mealType.MealTypeId);
        dto.Recipe.RecipeId.Should().Be(recipe.RecipeId);
    }

    [Test]
    public void StoreMapper_ToDto_MapsAllProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var articleGroup = context.ArticleGroups.Add(new EfArticleGroup("Vegetables")).Entity;
        var shoppingOrder = context.ShoppingOrders.Add(new EfShoppingOrder(articleGroup, 1)).Entity;
        var shoppingOrders = new List<EfShoppingOrder> { shoppingOrder };
        var entity = context.Stores.Add(new EfStore("Supermarket", shoppingOrders)).Entity;
        context.SaveChanges();

        // Act
        var dto = entity.ToDto();

        // Assert
        dto.StoreId.Should().Be(entity.StoreId);
        dto.Name.Should().Be(entity.Name);
    }

    [Test]
    public void ShoppingSessionMapper_ToSessionResponse_MapsAllProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var now = DateTimeOffset.UtcNow;
        var session = new EfShoppingSession("Tofu, Reis, Milch", now);
        context.ShoppingSessions.Add(session);
        context.SaveChanges();

        // Act
        var dto = session.ToDto();

        // Assert
        dto.SessionId.Should().Be(session.ShoppingSessionId.Value);
        dto.StartedAt.Should().Be(now);
        dto.Status.Should().Be("InProgress");
        dto.IngredientList.Should().Be("Tofu, Reis, Milch");
        dto.ItemCount.Should().Be(0);
    }

    [Test]
    public void ShoppingSessionMapper_ToSessionItemResponse_MapsAllProperties()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var now = DateTimeOffset.UtcNow;
        var session = new EfShoppingSession("Tofu", now);
        context.ShoppingSessions.Add(session);
        context.SaveChanges();

        var item = new EfShoppingSessionItem(
            "500g Tofu",
            session.ShoppingSessionId,
            now,
            "Bio Tofu Natur",
            "https://coop.ch/p/123",
            2,
            "2.95",
            global::DataLayer.EfClasses.SessionItemStatus.Added);
        context.ShoppingSessionItems.Add(item);
        context.SaveChanges();

        // Act
        var dto = item.ToDto();

        // Assert
        dto.OriginalIngredient.Should().Be("500g Tofu");
        dto.SelectedProductName.Should().Be("Bio Tofu Natur");
        dto.SelectedProductUrl.Should().Be("https://coop.ch/p/123");
        dto.Quantity.Should().Be(2);
        dto.Price.Should().Be("2.95");
        dto.Status.Should().Be("Added");
        dto.AddedAt.Should().Be(now);
    }

    [Test]
    public void ShoppingSessionMapper_ToDetailDto_MapsItemsCollection()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var now = DateTimeOffset.UtcNow;
        var session = new EfShoppingSession("Tofu, Reis", now);
        context.ShoppingSessions.Add(session);
        context.SaveChanges();

        var item1 = new EfShoppingSessionItem(
            "500g Tofu",
            session.ShoppingSessionId,
            now,
            "Bio Tofu",
            "https://coop.ch/p/1",
            1,
            "2.95");
        var item2 = new EfShoppingSessionItem(
            "1kg Reis",
            session.ShoppingSessionId,
            now,
            "Jasmin Reis",
            "https://coop.ch/p/2",
            1,
            "4.50");
        session.Items.Add(item1);
        session.Items.Add(item2);
        context.SaveChanges();

        // Act
        var dto = session.ToDetailDto();

        // Assert
        dto.SessionId.Should().Be(session.ShoppingSessionId.Value);
        dto.StartedAt.Should().Be(now);
        dto.IngredientList.Should().Be("Tofu, Reis");
        dto.Items.Should().HaveCount(2);
        dto.Items[0].OriginalIngredient.Should().Be("500g Tofu");
        dto.Items[1].OriginalIngredient.Should().Be("1kg Reis");
    }
}
