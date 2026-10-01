using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Models;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ProductModelsTests
{
    [Test]
    public void ProductDetails_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var details = new ProductDetails();

        // Assert
        details.Name.Should().Be(string.Empty);
        details.Price.Should().Be(string.Empty);
        details.Url.Should().Be(string.Empty);
        details.UnitSize.Should().Be(string.Empty);
        details.Brand.Should().Be(string.Empty);
        details.IsAvailable.Should().BeTrue();
        details.Description.Should().Be(string.Empty);
    }

    [Test]
    public void ProductDetails_CanSetAllProperties()
    {
        // Arrange & Act
        var details = new ProductDetails
        {
            Name = "Bio Milch",
            Price = "1.99",
            Url = "https://shop.example/product/1",
            UnitSize = "1L",
            Brand = "Coop",
            IsAvailable = false,
            Description = "Fresh organic milk",
        };

        // Assert
        details.Name.Should().Be("Bio Milch");
        details.Price.Should().Be("1.99");
        details.Url.Should().Be("https://shop.example/product/1");
        details.UnitSize.Should().Be("1L");
        details.Brand.Should().Be("Coop");
        details.IsAvailable.Should().BeFalse();
        details.Description.Should().Be("Fresh organic milk");
    }

    [Test]
    public void ProductDetails_EqualityByValue()
    {
        // Arrange
        var first = new ProductDetails { Name = "Bio Milch", Price = "1.99" };
        var second = new ProductDetails { Name = "Bio Milch", Price = "1.99" };

        // Act & Assert
        first.Should().Be(second);
    }

    [Test]
    public void ShopProduct_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var product = new ShopProduct();

        // Assert
        product.Name.Should().Be(string.Empty);
        product.Price.Should().Be(string.Empty);
        product.Url.Should().Be(string.Empty);
        product.ImageUrl.Should().Be(string.Empty);
        product.IsAvailable.Should().BeTrue();
    }

    [Test]
    public void ShopProduct_CanSetAllProperties()
    {
        // Arrange & Act
        var product = new ShopProduct
        {
            Name = "Bio Milch",
            Price = "1.99",
            Url = "https://shop.example/product/1",
            ImageUrl = "https://shop.example/image/1.jpg",
            IsAvailable = false,
        };

        // Assert
        product.Name.Should().Be("Bio Milch");
        product.Price.Should().Be("1.99");
        product.Url.Should().Be("https://shop.example/product/1");
        product.ImageUrl.Should().Be("https://shop.example/image/1.jpg");
        product.IsAvailable.Should().BeFalse();
    }

    [Test]
    public void ShopProduct_EqualityByValue()
    {
        // Arrange
        var first = new ShopProduct { Name = "Bio Milch", Price = "1.99" };
        var second = new ShopProduct { Name = "Bio Milch", Price = "1.99" };

        // Act & Assert
        first.Should().Be(second);
    }
}
