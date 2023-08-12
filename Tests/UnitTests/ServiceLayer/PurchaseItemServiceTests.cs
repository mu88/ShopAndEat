using BizLogic;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.PurchaseItem;
using DTO.Unit;
using Moq;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.UnitTests.ServiceLayer;

[TestFixture]
[Category("Unit")]
public class PurchaseItemServiceTests
{
    [Test]
    public void CreatePurchaseItem()
    {
        using var context = new InMemoryDbContext();
        var newPurchaseItemDto =
            new NewPurchaseItemDto(new ExistingArticleDto(1, "Tomato", new ExistingArticleGroupDto(1, "Vegetables"), false),
                                   new ExistingUnitDto(1, "Piece"),
                                   2);
        var purchaseItemActionMock = new Mock<IPurchaseItemAction>();
        var testee = new PurchaseItemService(purchaseItemActionMock.Object, context);

        testee.CreatePurchaseItem(newPurchaseItemDto);

        purchaseItemActionMock.Verify(x => x.CreatePurchaseItem(newPurchaseItemDto), Times.Once);
    }

    [Test]
    public void DeletePurchaseItem()
    {
        using var context = new InMemoryDbContext();
        var deletePurchaseItemGroupDto = new DeletePurchaseItemDto(3);
        var purchaseItemActionMock = new Mock<IPurchaseItemAction>();
        var testee = new PurchaseItemService(purchaseItemActionMock.Object, context);

        testee.DeletePurchaseItem(deletePurchaseItemGroupDto);

        purchaseItemActionMock.Verify(x => x.DeletePurchaseItem(deletePurchaseItemGroupDto), Times.Once);
    }
}