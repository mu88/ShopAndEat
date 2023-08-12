using BizLogic;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Unit;
using Moq;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.UnitTests.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class IngredientServiceTests
{
    [Test]
    public void CreateIngredient()
    {
        using var context = new InMemoryDbContext();
        var newIngredientDto =
            new NewIngredientDto(new ExistingArticleDto(1, "Tomato", new ExistingArticleGroupDto(1, "Vegetables"), false),
                                 2,
                                 new ExistingUnitDto(1, "Piece"));
        var ingredientActionMock = new Mock<IIngredientAction>();
        var testee = new IngredientService(ingredientActionMock.Object, context);

        testee.CreateIngredient(newIngredientDto);

        ingredientActionMock.Verify(x => x.CreateIngredient(newIngredientDto), Times.Once);
    }

    [Test]
    public void DeleteIngredient()
    {
        using var context = new InMemoryDbContext();
        var deleteIngredientGroupDto = new DeleteIngredientDto(3);
        var ingredientActionMock = new Mock<IIngredientAction>();
        var testee = new IngredientService(ingredientActionMock.Object, context);

        testee.DeleteIngredient(deleteIngredientGroupDto);

        ingredientActionMock.Verify(x => x.DeleteIngredient(deleteIngredientGroupDto), Times.Once);
    }

    [Test]
    public void GetAllIngredients()
    {
        using var context = new InMemoryDbContext();
        var ingredientActionMock = new Mock<IIngredientAction>();
        var testee = new IngredientService(ingredientActionMock.Object, context);

        testee.GetAllIngredients();

        ingredientActionMock.Verify(x => x.GetAllIngredients(), Times.Once);
    }
}