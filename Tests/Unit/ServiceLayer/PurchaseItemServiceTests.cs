using BizLogic;
using DTO.Article;
using DTO.ArticleGroup;
using DTO.PurchaseItem;
using DTO.Unit;
using NSubstitute;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Unit.ServiceLayer;

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
        var purchaseItemActionMock = Substitute.For<IPurchaseItemAction>();
        var testee = new PurchaseItemService(purchaseItemActionMock, context);

        testee.CreatePurchaseItem(newPurchaseItemDto);

        purchaseItemActionMock.Received(1).CreatePurchaseItem(newPurchaseItemDto);
    }

    [Test]
    public void DeletePurchaseItem()
    {
        using var context = new InMemoryDbContext();
        var deletePurchaseItemGroupDto = new DeletePurchaseItemDto(3);
        var purchaseItemActionMock = Substitute.For<IPurchaseItemAction>();
        var testee = new PurchaseItemService(purchaseItemActionMock, context);

        testee.DeletePurchaseItem(deletePurchaseItemGroupDto);

        purchaseItemActionMock.Received(1).DeletePurchaseItem(deletePurchaseItemGroupDto);
    }
}
