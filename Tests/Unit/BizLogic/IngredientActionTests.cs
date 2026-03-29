using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Unit;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class IngredientActionTests
{
    [Test]
    public void CreateIngredient()
    {
        // Arrange
        var newIngredientDto =
            new NewIngredientDto(new ExistingArticleDto(1, "Tomato", new ExistingArticleGroupDto(1, "Vegetables"), false),
                2,
                new ExistingUnitDto(1, "Piece"));
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        ingredientDbAccessMock.AddIngredient(Arg.Any<Ingredient>()).Returns(call => call.Arg<Ingredient>());
        var testee = new IngredientAction(ingredientDbAccessMock);

        // Act
        testee.CreateIngredient(newIngredientDto);

        // Assert
        ingredientDbAccessMock.Received(1).AddIngredient(Arg.Is<Ingredient>(a => a.Article.Name == "Tomato"));
    }

    [Test]
    public void DeleteIngredient()
    {
        // Arrange
        var deleteIngredientGroupDto = new DeleteIngredientDto(3);
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        ingredientDbAccessMock.GetIngredient(3)
            .Returns(new Ingredient(new Article { Name = "Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false },
                2,
                new global::DataLayer.EfClasses.Unit("Piece")));
        var testee = new IngredientAction(ingredientDbAccessMock);

        // Act
        testee.DeleteIngredient(deleteIngredientGroupDto);

        // Assert
        ingredientDbAccessMock.Received(1).DeleteIngredient(Arg.Is<Ingredient>(a => a.Article.Name == "Tomato"));
    }

    [Test]
    public void GetAllIngredients()
    {
        // Arrange
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        var testee = new IngredientAction(ingredientDbAccessMock);

        // Act
        testee.GetAllIngredients();

        // Assert
        ingredientDbAccessMock.Received(1).GetIngredients();
    }
}
