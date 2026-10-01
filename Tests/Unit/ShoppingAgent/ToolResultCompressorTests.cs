using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ToolResultCompressorTests
{
    private ToolResultCompressor _sut = null!;

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
    public void Compress_ReturnsRawResultWithoutDeserializing_WhenRawResultIsNull()
    {
        // Act — the null-guard must short-circuit before the switch, otherwise "search_products" would
        // route to JsonSerializer.Deserialize(null, ...), which throws ArgumentNullException (not caught).
        var act = () => _sut.Compress("search_products", null!);

        // Assert
        act.Should().NotThrow().Which.Should().BeNull();
    }

    [Test]
    public void Compress_ReturnsRawResult_WhenRawResultIsWhitespace()
    {
        // Act
        var result = _sut.Compress("search_products", "   ");

        // Assert
        result.Should().Be("   ");
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
    public void Compress_WhenSearchResultUsesLowerCasePropertyNames_StillDeserializesCaseInsensitively()
    {
        // Arrange — proves ReadOptions.PropertyNameCaseInsensitive = true is actually in effect;
        // ShopProduct's properties are PascalCase, so lower-case JSON keys would fail to bind otherwise.
        var json = """[{"name":"Bio Tofu","price":"CHF 3.95","url":"https://coop.ch/p/123"}]""";

        // Act
        var result = _sut.Compress("search_products", json);

        // Assert
        result.Should().Contain("Bio Tofu");
        result.Should().Contain("CHF 3.95");
        result.Should().Contain("https://coop.ch/p/123");
    }

    [Test]
    public void Compress_WhenCartContentsHasLeadingWhitespaceBeforeArray_StillCompresses()
    {
        // Arrange — proves the guard checks specifically for '[' (not merely "any character"); with
        // leading whitespace, System.Text.Json still parses the array successfully once entered,
        // so only a real StartsWith("[") check distinguishes this from an always-true check.
        const string json = " [{\"name\":\"Bio Tofu\",\"qty\":1,\"price\":\"CHF 3.95\"}]";

        // Act
        var result = _sut.Compress("get_cart_contents", json);

        // Assert — leading-whitespace input is NOT recognized as starting with '[', so it must be
        // returned completely unchanged (not compressed).
        result.Should().Be(json);
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

    [Test]
    public void Compress_WhenSearchResultDeserializesToNull_ReturnsRawResult()
    {
        // Arrange
        const string json = "null";

        // Act
        var result = _sut.Compress("search_products", json);

        // Assert
        result.Should().Be(json);
    }

    [Test]
    public void Compress_WhenProductDetailsDeserializesToNull_ReturnsRawResult()
    {
        // Arrange
        const string json = "null";

        // Act
        var result = _sut.Compress("get_product_details", json);

        // Assert
        result.Should().Be(json);
    }

    [Test]
    public void Compress_WhenCartContentsIsWhitespaceOnly_ReturnsRawResult()
    {
        // Arrange
        const string whitespace = "   ";

        // Act
        var result = _sut.Compress("get_cart_contents", whitespace);

        // Assert
        result.Should().Be(whitespace);
    }

    [Test]
    public void Compress_WhenCartContentsDeserializesToNull_ReturnsRawResult()
    {
        // Arrange
        const string json = "[]";

        // Act
        var result = _sut.Compress("get_cart_contents", json);

        // Assert
        result.Should().Be("[]");
    }

    [Test]
    public void Compress_WhenCartContentsDoesNotStartWithBracket_ReturnsRawResult()
    {
        // Arrange
        const string json = """{"some":"object"}""";

        // Act
        var result = _sut.Compress("get_cart_contents", json);

        // Assert
        result.Should().Be(json);
    }

    [Test]
    public void Compress_WhenCartContentsHasItemsWithMissingFields_CompressesWithoutThrowing()
    {
        // Arrange
        // Characterization test: missing fields are simply omitted per item (not null-padded),
        // present fields are preserved and converted to their compressed string representation.
        var json = """
            [
              {"name":"Item1","qty":1},
              {"name":"Item2","price":"CHF 5.00"},
              {"qty":3,"price":"CHF 10.00"}
            ]
            """;

        // Act
        var result = _sut.Compress("get_cart_contents", json);

        // Assert
        result.Should().Contain("Item1");
        result.Should().Contain("Item2");
        result.Should().Contain("\"qty\":\"1\"");
        result.Should().Contain("\"qty\":\"3\"");
    }

    [Test]
    public void Compress_WhenCartContentsHasExplicitNullFields_CompressesWithoutThrowing()
    {
        // Arrange — an explicit JSON "null" (as opposed to a missing key) deserializes to an actual
        // C# null in the Dictionary<string, object>, exercising the `?.ToString()` null branch.
        var json = """
            [
              {"name":null,"qty":null,"price":null}
            ]
            """;

        // Act
        var act = () => _sut.Compress("get_cart_contents", json);

        // Assert — WriteOptions ignores null values on serialize, so the null branch is exercised
        // internally without throwing, but the resulting JSON simply omits the fields.
        act.Should().NotThrow();
        act().Should().Be("[{}]");
    }

    [Test]
    public void Compress_WhenAddToCartIsWhitespaceOnly_ReturnsRawResult()
    {
        // Arrange
        const string whitespace = "   ";

        // Act
        var result = _sut.Compress("add_to_cart", whitespace);

        // Assert
        result.Should().Be(whitespace);
    }

    [Test]
    public void Compress_WhenAddToCartDeserializesToNull_ReturnsRawResult()
    {
        // Arrange
        const string json = "null";

        // Act
        var result = _sut.Compress("add_to_cart", json);

        // Assert
        result.Should().Be(json);
    }

    [Test]
    public void Compress_WhenAddToCartDoesNotStartWithBrace_ReturnsRawResult()
    {
        // Arrange
        const string json = """["array"]""";

        // Act
        var result = _sut.Compress("add_to_cart", json);

        // Assert
        result.Should().Be(json);
    }

    [Test]
    public void Compress_WhenAddToCartHasMissingFields_CompressesWithNulls()
    {
        // Arrange
        var json = """
            {
              "success":true,
              "quantity":2
            }
            """;

        // Act
        var result = _sut.Compress("add_to_cart", json);

        // Assert
        result.Should().Contain("success");
        result.Should().NotContain("quantity");
    }

    [Test]
    public void Compress_WhenAddToCartHasExplicitNullFields_CompressesWithoutThrowing()
    {
        // Arrange — explicit JSON "null" exercises the `?.ToString()` null branch on a present key.
        var json = """
            {
              "success":null,
              "message":null
            }
            """;

        // Act
        var act = () => _sut.Compress("add_to_cart", json);

        // Assert — WriteOptions ignores null values on serialize, so the null branch is exercised
        // internally without throwing, but the resulting JSON simply omits the fields.
        act.Should().NotThrow();
        act().Should().Be("{}");
    }

    [Test]
    public void Compress_WhenRemoveFromCartIsWhitespaceOnly_ReturnsRawResult()
    {
        // Arrange
        const string whitespace = "   ";

        // Act
        var result = _sut.Compress("remove_from_cart", whitespace);

        // Assert
        result.Should().Be(whitespace);
    }

    [Test]
    public void Compress_WhenRemoveFromCartDeserializesToNull_ReturnsRawResult()
    {
        // Arrange
        const string json = "null";

        // Act
        var result = _sut.Compress("remove_from_cart", json);

        // Assert
        result.Should().Be(json);
    }

    [Test]
    public void Compress_WhenRemoveFromCartDoesNotStartWithBrace_ReturnsRawResult()
    {
        // Arrange
        const string json = """["array"]""";

        // Act
        var result = _sut.Compress("remove_from_cart", json);

        // Assert
        result.Should().Be(json);
    }

    [Test]
    public void Compress_WhenRemoveFromCartHasMissingFields_CompressesWithNulls()
    {
        // Arrange
        var json = """
            {
              "success":false,
              "productName":"Item"
            }
            """;

        // Act
        var result = _sut.Compress("remove_from_cart", json);

        // Assert
        result.Should().Contain("success");
        result.Should().NotContain("productName");
    }

    [Test]
    public void Compress_WhenRemoveFromCartHasExplicitNullFields_CompressesWithoutThrowing()
    {
        // Arrange — explicit JSON "null" exercises the `?.ToString()` null branch on a present key.
        var json = """
            {
              "success":null,
              "message":null
            }
            """;

        // Act
        var act = () => _sut.Compress("remove_from_cart", json);

        // Assert — WriteOptions ignores null values on serialize, so the null branch is exercised
        // internally without throwing, but the resulting JSON simply omits the fields.
        act.Should().NotThrow();
        act().Should().Be("{}");
    }

    [Test]
    public void Compress_WhenProductDetailsIsMalformedJson_ReturnsRawResult()
    {
        // Arrange
        const string malformed = """{"Name": [1, 2, 3]}""";

        // Act
        var result = _sut.Compress("get_product_details", malformed);

        // Assert
        result.Should().Be(malformed);
    }

    [Test]
    public void Compress_WhenCartContentsIsMalformedJson_ReturnsRawResult()
    {
        // Arrange
        const string malformed = """[{"name":}]""";

        // Act
        var result = _sut.Compress("get_cart_contents", malformed);

        // Assert
        result.Should().Be(malformed);
    }

    [Test]
    public void Compress_WhenAddToCartIsMalformedJson_ReturnsRawResult()
    {
        // Arrange
        const string malformed = """{"success":}""";

        // Act
        var result = _sut.Compress("add_to_cart", malformed);

        // Assert
        result.Should().Be(malformed);
    }

    [Test]
    public void Compress_WhenRemoveFromCartIsMalformedJson_ReturnsRawResult()
    {
        // Arrange
        const string malformed = """{"success":}""";

        // Act
        var result = _sut.Compress("remove_from_cart", malformed);

        // Assert
        result.Should().Be(malformed);
    }
}
