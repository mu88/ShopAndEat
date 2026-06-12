using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ToolResultCompressorTests
{
    private ToolResultCompressor _sut;

    [SetUp]
    public void SetUp() => _sut = new ToolResultCompressor();

    [Test]
    public void Compress_WhenToolIsSearchProducts_ReturnsTrimmedTopFiveResults()
    {
        // Arrange
        var json = """
            [
              {"Name":"A","Price":"CHF 1.00","Url":"https://a","ImageUrl":"img","IsAvailable":true},
              {"Name":"B","Price":"CHF 2.00","Url":"https://b","ImageUrl":"img","IsAvailable":true},
              {"Name":"C","Price":"CHF 3.00","Url":"https://c","ImageUrl":"img","IsAvailable":true},
              {"Name":"D","Price":"CHF 4.00","Url":"https://d","ImageUrl":"img","IsAvailable":true},
              {"Name":"E","Price":"CHF 5.00","Url":"https://e","ImageUrl":"img","IsAvailable":true},
              {"Name":"F","Price":"CHF 6.00","Url":"https://f","ImageUrl":"img","IsAvailable":true}
            ]
            """;

        // Act
        var result = _sut.Compress("search_products", json);

        // Assert
        result.Should().NotContain("\"F\"");
        result.Should().Contain("\"A\"");
        result.Should().Contain("\"E\"");
        result.Should().NotContain("ImageUrl");
        result.Should().NotContain("IsAvailable");
    }

    [Test]
    public void Compress_WhenToolIsSearchProducts_KeepsNamePriceAndUrl()
    {
        // Arrange
        var json = """[{"Name":"Bio Tofu","Price":"CHF 3.95","Url":"https://coop.ch/p/123","ImageUrl":"img","IsAvailable":true}]""";

        // Act
        var result = _sut.Compress("search_products", json);

        // Assert
        result.Should().Contain("Bio Tofu");
        result.Should().Contain("CHF 3.95");
        result.Should().Contain("https://coop.ch/p/123");
    }

    [Test]
    public void Compress_WhenToolIsSearchProductsWithEmptyArray_ReturnsEmptyArray()
    {
        // Act
        var result = _sut.Compress("search_products", "[]");

        // Assert
        result.Should().Be("[]");
    }

    [Test]
    public void Compress_WhenToolIsGetProductDetails_KeepsNamePriceUnitSizeAndUrl()
    {
        // Arrange
        var json = """
            {
              "Name":"Bio Tofu","Price":"CHF 3.95","Url":"https://coop.ch/p/123",
              "UnitSize":"200g","Brand":"Karma","IsAvailable":true,"Description":"Fresh tofu"
            }
            """;

        // Act
        var result = _sut.Compress("get_product_details", json);

        // Assert
        result.Should().Contain("Bio Tofu");
        result.Should().Contain("CHF 3.95");
        result.Should().Contain("https://coop.ch/p/123");
        result.Should().Contain("200g");
        result.Should().NotContain("Description");
        result.Should().NotContain("Brand");
    }

    [Test]
    public void Compress_WhenToolIsGetCartContents_KeepsOnlyNameQtyAndPrice()
    {
        // Arrange
        var json = """
            [
              {"name":"Bio Tofu","qty":2,"price":"CHF 7.90","uid":"uid-1","removed":false},
              {"name":"Pasta","qty":1,"price":"CHF 2.50","uid":"uid-2","removed":false}
            ]
            """;

        // Act
        var result = _sut.Compress("get_cart_contents", json);

        // Assert
        result.Should().Contain("Bio Tofu");
        result.Should().Contain("Pasta");
        result.Should().Contain("CHF 7.90");
        result.Should().NotContain("uid");
        result.Should().NotContain("removed");
    }

    [Test]
    public void Compress_WhenToolIsAddToCart_KeepsOnlySuccessAndMessage()
    {
        // Arrange
        var json = """
            {
              "success":true,"message":"Added to cart","quantity":2,"productUrl":"https://coop.ch/p/123"
            }
            """;

        // Act
        var result = _sut.Compress("add_to_cart", json);

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("message");
        result.Should().Contain("Added to cart");
        result.Should().NotContain("productUrl");
    }

    [Test]
    public void Compress_WhenToolIsRemoveFromCart_KeepsOnlySuccessAndMessage()
    {
        // Arrange
        var json = """
            {
              "success":true,"message":"Removed from cart","productName":"Bio Tofu","qty":2
            }
            """;

        // Act
        var result = _sut.Compress("remove_from_cart", json);

        // Assert
        result.Should().Contain("success");
        result.Should().Contain("message");
        result.Should().Contain("Removed from cart");
        result.Should().NotContain("productName");
        result.Should().NotContain("qty");
    }

    [Test]
    public void Compress_WhenToolIsUnknown_ReturnsRawResult()
    {
        // Arrange
        const string raw = "some raw result";

        // Act
        var result = _sut.Compress("verify_shopping_list", raw);

        // Assert
        result.Should().Be(raw);
    }

    [Test]
    public void Compress_WhenSearchResultIsInvalidJson_ReturnsRawResult()
    {
        // Arrange
        const string notJson = "not valid json at all";

        // Act
        var result = _sut.Compress("search_products", notJson);

        // Assert
        result.Should().Be(notJson);
    }

    [Test]
    public void Compress_WhenResultIsEmpty_ReturnsEmpty()
    {
        // Act
        var result = _sut.Compress("search_products", string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void Compress_WhenGetCartContentsIsInvalidJson_ReturnsRawResult()
    {
        // Arrange
        const string notJson = "not valid json at all";

        // Act
        var result = _sut.Compress("get_cart_contents", notJson);

        // Assert
        result.Should().Be(notJson);
    }
}
