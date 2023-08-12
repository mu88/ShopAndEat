using BizDbAccess;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.PurchaseItem;
using DTO.Unit;
using Moq;
using NUnit.Framework;
using Tests.Doubles;

namespace Tests.UnitTests.BizLogic;

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
        var purchaseItemDbAccessMock = new Mock<IPurchaseItemDbAccess>();
        var testee = new PurchaseItemAction(purchaseItemDbAccessMock.Object, TestMapper.Create());

        testee.CreatePurchaseItem(newPurchaseItemDto);

        purchaseItemDbAccessMock.Verify(x => x.AddPurchaseItem(It.Is<PurchaseItem>(a => a.Article.Name == "Tomato")), Times.Once);
    }

    [Test]
    public void DeletePurchaseItem()
    {
        var deletePurchaseItemGroupDto = new DeletePurchaseItemDto(3);
        var purchaseItemDbAccessMock = new Mock<IPurchaseItemDbAccess>();
        purchaseItemDbAccessMock.Setup(x => x.GetPurchaseItem(3))
            .Returns(new PurchaseItem(new Article{Name="Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false},
                                      2,
                                      new Unit("Piece")));
        var testee = new PurchaseItemAction(purchaseItemDbAccessMock.Object, TestMapper.Create());

        testee.DeletePurchaseItem(deletePurchaseItemGroupDto);

        purchaseItemDbAccessMock.Verify(x => x.DeletePurchaseItem(It.Is<PurchaseItem>(a => a.Article.Name == "Tomato")), Times.Once);
    }
}