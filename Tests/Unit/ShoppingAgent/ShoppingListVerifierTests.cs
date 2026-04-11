using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ShoppingListVerifierTests
{
    private ShoppingListVerifier _sut;

    [SetUp]
    public void SetUp() => _sut = new ShoppingListVerifier();

    [Test]
    public void FindMissingItems_EmptyShoppingList_ReturnsEmpty()
    {
        // Arrange
        var cartContents = """[{"Name":"Naturaplan Bio Mozzarella","Price":"CHF 2.40"}]""";

        // Act
        var result = _sut.FindMissingItems(string.Empty, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_EmptyCartContents_ReturnsEmpty()
    {
        // Arrange
        var shoppingList = "1 Packung Mozzarella";

        // Act
        var result = _sut.FindMissingItems(shoppingList, string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_NullShoppingList_ReturnsEmpty()
    {
        // Arrange
        var cartContents = """[{"Name":"Mozzarella"}]""";

        // Act
        var result = _sut.FindMissingItems(null, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_AllItemsFound_ReturnsEmpty()
    {
        // Arrange
        var shoppingList = """
            2 Packungen Toast
            1 Packung Mozzarella
            600 Gramm Spiralen
            """;
        var cartContents = """
            [
              {"Name":"Naturaplan Bio Vollkorntoast"},
              {"Name":"Naturaplan Bio Mozzarella"},
              {"Name":"Naturaplan Bio Spiralen 3-Eier"}
            ]
            """;

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_MissingItem_ReturnsMissingKeyword()
    {
        // Arrange
        var shoppingList = """
            2 Packungen Toast
            1 Stück Lauch
            """;
        var cartContents = """[{"Name":"Naturaplan Bio Vollkorntoast"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().ContainSingle().Which.Should().Be("Lauch");
    }

    [Test]
    public void FindMissingItems_MultipleMissingItems_ReturnsAll()
    {
        // Arrange
        var shoppingList = """
            2 Stück Kartoffeln
            1 Stück Lauch
            3 Stück Paprika
            1 Packung Rucola
            """;
        var cartContents = """[{"Name":"Naturaplan Bio Rucola"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().HaveCount(3)
            .And.Contain("Kartoffeln")
            .And.Contain("Lauch")
            .And.Contain("Paprika");
    }

    [Test]
    public void FindMissingItems_StripsQuantityAndUnit_BeforeMatching()
    {
        // Arrange
        var shoppingList = "75 Gramm Parmesan";
        var cartContents = """[{"Name":"Naturaplan Bio Parmesan"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_StripsParentheticalNote_BeforeMatching()
    {
        // Arrange
        // Parenthetical is extra info, keyword before parens is used for matching
        var shoppingList = "1 Packung Milch (Bio)";
        var cartContents = """[{"Name":"Naturaplan Bio Vollmilch"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_ItemWithDoseUnit_Stripped()
    {
        // Arrange
        var shoppingList = "1 Dose Gehackte Tomaten";
        var cartContents = """[{"Name":"Gehackte Tomaten"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_CaseInsensitiveMatch()
    {
        // Arrange
        var shoppingList = "1 Packung MOZZARELLA";
        var cartContents = """[{"Name":"Naturaplan Bio Mozzarella"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_ShortWords_NotMatchedAlone()
    {
        // Arrange
        // "Eis" has only 3 characters → should not match any cart entry
        var shoppingList = "1 Packung Eis";
        var cartContents = """[{"Name":"Naturaplan Bio Vollkorntoast"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().ContainSingle().Which.Should().Be("Eis");
    }

    [Test]
    public void FindMissingItems_WhitespaceOnlyLine_IsIgnored()
    {
        // Arrange — "   " is not removed by RemoveEmptyEntries but Trim() makes it empty → continue
        var shoppingList = "1 Packung Mozzarella\n   \n1 Stück Lauch";
        var cartContents = """[{"Name":"Mozzarella"},{"Name":"Lauch"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_QuantityOnlyLine_IsIgnored()
    {
        // Arrange — "3" stripped by regex leaves empty keyword → continue
        var shoppingList = "3\n1 Packung Mozzarella";
        var cartContents = """[{"Name":"Mozzarella"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_BlankLines_Ignored()
    {
        // Arrange
        var shoppingList = """

            1 Packung Mozzarella

            """;
        var cartContents = """[{"Name":"Naturaplan Bio Mozzarella"}]""";

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public void FindMissingItems_MixedQuantityFormats_HandledCorrectly()
    {
        // Arrange
        var shoppingList = """
            2x Äpfel
            1,5 kg Mehl
            3.0 Packungen Joghurt
            """;
        var cartContents = """
            [
              {"Name":"Naturaplan Bio Äpfel"},
              {"Name":"Prix Garantie Weissmehl"},
              {"Name":"Naturaplan Bio Joghurt"}
            ]
            """;

        // Act
        var result = _sut.FindMissingItems(shoppingList, cartContents);

        // Assert
        result.Should().BeEmpty();
    }
}
