using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.PurchaseItem;
using DTO.Unit;
using NSubstitute;
using NUnit.Framework;

namespace Tests.Unit.BizLogic;

[TestFixture]
[Category("Unit")]
public class PurchaseItemActionTests
{
    [Test]
    public void CreatePurchaseItem()
    {
        var newPurchaseItemDto =
            new NewPurchaseItemDto(new ExistingArticleDto(1, "Tomato", new ExistingArticleGroupDto(1, "Vegetables"), false),
                new ExistingUnitDto(1, "Piece"),
                2);
        var purchaseItemDbAccessMock = Substitute.For<IPurchaseItemDbAccess>();
        purchaseItemDbAccessMock.AddPurchaseItem(Arg.Any<PurchaseItem>()).Returns(call => call.Arg<PurchaseItem>());
        var testee = new PurchaseItemAction(purchaseItemDbAccessMock);

        testee.CreatePurchaseItem(newPurchaseItemDto);

        purchaseItemDbAccessMock.Received(1).AddPurchaseItem(Arg.Is<PurchaseItem>(a => a.Article.Name == "Tomato"));
    }

    [Test]
    public void DeletePurchaseItem()
    {
        var deletePurchaseItemGroupDto = new DeletePurchaseItemDto(3);
        var purchaseItemDbAccessMock = Substitute.For<IPurchaseItemDbAccess>();
        purchaseItemDbAccessMock.GetPurchaseItem(3)
            .Returns(new PurchaseItem(new Article { Name = "Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false },
                2,
                new global::DataLayer.EfClasses.Unit("Piece")));
        var testee = new PurchaseItemAction(purchaseItemDbAccessMock);

        testee.DeletePurchaseItem(deletePurchaseItemGroupDto);

        purchaseItemDbAccessMock.Received(1).DeletePurchaseItem(Arg.Is<PurchaseItem>(a => a.Article.Name == "Tomato"));
    }
}
