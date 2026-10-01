using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.PurchaseItem;
using DTO.Unit;
using NSubstitute;
using NUnit.Framework;
using Tests.Builders;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class PurchaseItemActionTests
{
    [Test]
    public void CreatePurchaseItem()
    {
        // Arrange
        var newPurchaseItemDto =
            new NewPurchaseItemDto(new ExistingArticleDto(new global::DataLayer.EfClasses.ArticleId(1), "Tomato", new ExistingArticleGroupDto(new global::DataLayer.EfClasses.ArticleGroupId(1), "Vegetables"), false),
                new ExistingUnitDto(new global::DataLayer.EfClasses.UnitId(1), "Piece"),
                2);
        var purchaseItemDbAccessMock = Substitute.For<IPurchaseItemDbAccess>();
        purchaseItemDbAccessMock.AddPurchaseItem(Arg.Any<PurchaseItem>()).Returns(call => call.Arg<PurchaseItem>());
        var testee = new PurchaseItemAction(purchaseItemDbAccessMock);

        // Act
        testee.CreatePurchaseItem(newPurchaseItemDto);

        // Assert
        purchaseItemDbAccessMock.Received(1).AddPurchaseItem(Arg.Is<PurchaseItem>(a => a.Article.Name == "Tomato"));
    }

    [Test]
    public async Task DeletePurchaseItemAsync()
    {
        // Arrange
        var deletePurchaseItemGroupDto = new DeletePurchaseItemDto(3);
        var purchaseItemDbAccessMock = Substitute.For<IPurchaseItemDbAccess>();
        purchaseItemDbAccessMock.GetPurchaseItemAsync(3)
            .Returns(Task.FromResult(new PurchaseItemBuilder().WithDefaults().Build()));
        var testee = new PurchaseItemAction(purchaseItemDbAccessMock);

        // Act
        await testee.DeletePurchaseItemAsync(deletePurchaseItemGroupDto);

        // Assert
        purchaseItemDbAccessMock.Received(1).DeletePurchaseItem(Arg.Is<PurchaseItem>(a => a.Article.Name == "Tomato"));
    }
}
