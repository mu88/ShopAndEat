using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Unit;
using NSubstitute;
using NUnit.Framework;
using Tests.Doubles;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class IngredientActionTests
{
    [Test]
    public void CreateIngredient()
    {
        var newIngredientDto =
            new NewIngredientDto(new ExistingArticleDto(1, "Tomato", new ExistingArticleGroupDto(1, "Vegetables"), false),
                2,
                new ExistingUnitDto(1, "Piece"));
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        var testee = new IngredientAction(ingredientDbAccessMock, TestMapper.Create());

        testee.CreateIngredient(newIngredientDto);

        ingredientDbAccessMock.Received(1).AddIngredient(Arg.Is<Ingredient>(a => a.Article.Name == "Tomato"));
    }

    [Test]
    public void DeleteIngredient()
    {
        var deleteIngredientGroupDto = new DeleteIngredientDto(3);
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        ingredientDbAccessMock.GetIngredient(3)
            .Returns(new Ingredient(new Article { Name = "Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false },
                2,
                new global::DataLayer.EfClasses.Unit("Piece")));
        var testee = new IngredientAction(ingredientDbAccessMock, TestMapper.Create());

        testee.DeleteIngredient(deleteIngredientGroupDto);

        ingredientDbAccessMock.Received(1).DeleteIngredient(Arg.Is<Ingredient>(a => a.Article.Name == "Tomato"));
    }

    [Test]
    public void GetAllIngredients()
    {
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        var testee = new IngredientAction(ingredientDbAccessMock, TestMapper.Create());

        testee.GetAllIngredients();

        ingredientDbAccessMock.Received(1).GetIngredients();
    }
}
