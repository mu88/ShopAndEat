using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.Ingredient;
using DTO.Unit;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Tests.Builders;

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
            new NewIngredientDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables"), false),
                2,
                new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "Piece"));
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        ingredientDbAccessMock.AddIngredient(Arg.Any<Ingredient>()).Returns(call => call.Arg<Ingredient>());
        var testee = new IngredientAction(ingredientDbAccessMock);

        // Act
        testee.CreateIngredient(newIngredientDto);

        // Assert
        ingredientDbAccessMock.Received(1).AddIngredient(Arg.Is<Ingredient>(a => a.Article.Name == "Tomato"));
    }

    [Test]
    public async Task DeleteIngredientAsync()
    {
        // Arrange
        var deleteIngredientGroupDto = new DeleteIngredientDto(3);
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        ingredientDbAccessMock.GetIngredientAsync(3)
            .Returns(Task.FromResult(new IngredientBuilder().WithDefaults().Build()));
        var testee = new IngredientAction(ingredientDbAccessMock);

        // Act
        await testee.DeleteIngredientAsync(deleteIngredientGroupDto);

        // Assert
        ingredientDbAccessMock.Received(1).DeleteIngredient(Arg.Is<Ingredient>(a => a.Article.Name == "Tomato"));
    }

    [Test]
    public async Task GetAllIngredientsAsync()
    {
        // Arrange
        var ingredientDbAccessMock = Substitute.For<IIngredientDbAccess>();
        var ingredient = new IngredientBuilder().WithDefaults().Build();
        ingredientDbAccessMock.GetIngredientsAsync().Returns(Task.FromResult<IEnumerable<Ingredient>>(new[] { ingredient }));
        var testee = new IngredientAction(ingredientDbAccessMock);

        // Act
        var result = await testee.GetAllIngredientsAsync();

        // Assert
        await ingredientDbAccessMock.Received(1).GetIngredientsAsync();
        result.Should().ContainSingle(dto => dto.Article.Name == "Tomato");
    }
}
