using BizLogic;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Unit;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Unit.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class IngredientServiceTests
{
    [Test]
    public void CreateIngredient()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var newIngredientDto =
            new NewIngredientDto(new ExistingArticleDto(1, "Tomato", new ExistingArticleGroupDto(1, "Vegetables"), false),
                2,
                new ExistingUnitDto(1, "Piece"));
        var ingredientActionMock = Substitute.For<IIngredientAction>();
        var testee = new IngredientService(ingredientActionMock, context);

        // Act
        testee.CreateIngredient(newIngredientDto);

        // Assert
        ingredientActionMock.Received(1).CreateIngredient(newIngredientDto);
    }

    [Test]
    public void DeleteIngredient()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var deleteIngredientGroupDto = new DeleteIngredientDto(3);
        var ingredientActionMock = Substitute.For<IIngredientAction>();
        var testee = new IngredientService(ingredientActionMock, context);

        // Act
        testee.DeleteIngredient(deleteIngredientGroupDto);

        // Assert
        ingredientActionMock.Received(1).DeleteIngredient(deleteIngredientGroupDto);
    }

    [Test]
    public void GetAllIngredients()
    {
        // Arrange
        using var context = new InMemoryDbContext();
        var ingredientActionMock = Substitute.For<IIngredientAction>();
        var testee = new IngredientService(ingredientActionMock, context);

        // Act
        testee.GetAllIngredients();

        // Assert
        ingredientActionMock.Received(1).GetAllIngredients();
    }
}
