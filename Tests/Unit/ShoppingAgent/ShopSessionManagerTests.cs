using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Models;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ShopSessionManagerTests
{
    private IShopToolExecutorFactory _factoryMock = null!;
    private ILogger<ShopSessionManager> _loggerMock = null!;
    private ShopSessionManager _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _factoryMock = Substitute.For<IShopToolExecutorFactory>();
        _loggerMock = Substitute.For<ILogger<ShopSessionManager>>();
    }

    private void CreateSut(IReadOnlyList<ShopConfig> shops)
    {
        _factoryMock.AvailableShops.Returns(shops);
        _sut = new ShopSessionManager(_factoryMock, _loggerMock);
    }

    [Test]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var shops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };

        // Act
        CreateSut(shops);

        // Assert
        _sut.Should().NotBeNull();
    }

    [Test]
    public void InitialState_IsNotInitialized()
    {
        // Arrange
        var shops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };

        // Act
        CreateSut(shops);

        // Assert
        _sut.IsInitialized.Should().BeFalse();
    }

    [Test]
    public void InitialState_SelectedShopKeyIsNull()
    {
        // Arrange
        var shops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };

        // Act
        CreateSut(shops);

        // Assert
        _sut.SelectedShopKey.Should().BeNull();
    }

    [Test]
    public void InitialState_SelectedShopIsNull()
    {
        // Arrange
        var shops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };

        // Act
        CreateSut(shops);

        // Assert
        _sut.SelectedShop.Should().BeNull();
    }

    [Test]
    public void AvailableShops_ReturnsFactoryShops()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };

        // Act
        CreateSut(shops);

        // Assert
        _sut.AvailableShops.Should().BeEquivalentTo(shops);
    }

    [Test]
    public void SelectShop_WithNullShopKey_SelectsFirstShop()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop(null);

        // Assert
        _sut.SelectedShopKey.Should().Be("coop");
        _sut.SelectedShop.Should().Be(shops[0]);
    }

    [Test]
    public void SelectShop_WithoutParameters_SelectsFirstShop()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop();

        // Assert
        _sut.SelectedShopKey.Should().Be("coop");
        _sut.SelectedShop.Should().Be(shops[0]);
    }

    [Test]
    public void SelectShop_WithNullShopKey_SetsIsInitializedToTrue()
    {
        // Arrange
        var shops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };
        CreateSut(shops);

        // Act
        _sut.SelectShop(null);

        // Assert
        _sut.IsInitialized.Should().BeTrue();
    }

    [Test]
    public void SelectShop_WithNullShopKey_LogsInitialization()
    {
        // Arrange
        var shops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };
        CreateSut(shops);

        // Act
        _sut.SelectShop(null);

        // Assert
        // CA1873 false positive: this verifies an NSubstitute mock invocation of ILogger.Log,
        // not a real logging call, so lazy IsEnabled evaluation does not apply here.
#pragma warning disable CA1873
        _loggerMock.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CA1873
    }

    [Test]
    public void SelectShop_WithValidShopKey_SelectsCorrectShop()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop("migros");

        // Assert
        _sut.SelectedShopKey.Should().Be("migros");
        _sut.SelectedShop.Should().Be(shops[1]);
    }

    [Test]
    public void SelectShop_WithValidShopKey_SetsIsInitializedToTrue()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop("migros");

        // Assert
        _sut.IsInitialized.Should().BeTrue();
    }

    [Test]
    public void SelectShop_WithValidShopKey_LogsInitialization()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop("migros");

        // Assert
        // CA1873 false positive: this verifies an NSubstitute mock invocation of ILogger.Log,
        // not a real logging call, so lazy IsEnabled evaluation does not apply here.
#pragma warning disable CA1873
        _loggerMock.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
#pragma warning restore CA1873
    }

    [Test]
    public void SelectShop_WithValidShopKey_MatchesKeyCaseSensitively()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop("migros");

        // Assert
        _sut.SelectedShopKey.Should().Be("migros");
        _sut.SelectedShop!.Key.Should().Be("migros");
    }

    [Test]
    public void SelectShop_WithFirstShop_SelectsCorrectly()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop("coop");

        // Assert
        _sut.SelectedShopKey.Should().Be("coop");
        _sut.SelectedShop.Should().Be(shops[0]);
    }

    [Test]
    public void SelectShop_WithNonExistentShopKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        var act = () => _sut.SelectShop("unknown");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void SelectShop_WithInvalidShopKey_LeavesSelectedShopKeyMutatedButSelectedShopUnchanged()
    {
        // Arrange
        // Characterization test: SelectShop assigns SelectedShopKey BEFORE looking up SelectedShop,
        // so an invalid key mutates SelectedShopKey even though the lookup then throws.
        // This documents the current (buggy) behavior as-is; it is not the desired behavior.
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);
        _sut.SelectShop("coop");
        var previouslySelectedShop = _sut.SelectedShop;

        // Act
        var act = () => _sut.SelectShop("invalid");

        // Assert
        act.Should().Throw<InvalidOperationException>();
        _sut.SelectedShopKey.Should().Be("invalid");
        _sut.SelectedShop.Should().Be(previouslySelectedShop);
        _sut.IsInitialized.Should().BeTrue();
    }

    [Test]
    public void SelectShop_WithCaseSensitiveMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);

        // Act
        var act = () => _sut.SelectShop("COOP");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void SelectShop_CalledTwiceWithDifferentKeys_UpdatesSelection()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);
        _sut.SelectShop("coop");

        // Act
        _sut.SelectShop("migros");

        // Assert
        _sut.SelectedShopKey.Should().Be("migros");
        _sut.SelectedShop.Should().Be(shops[1]);
        _sut.IsInitialized.Should().BeTrue();
    }

    [Test]
    public void SelectShop_CalledTwiceWithSameKey_RemainsConsistent()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);
        _sut.SelectShop("coop");

        // Act
        _sut.SelectShop("coop");

        // Assert
        _sut.SelectedShopKey.Should().Be("coop");
        _sut.SelectedShop.Should().Be(shops[0]);
        _sut.IsInitialized.Should().BeTrue();
    }

    [Test]
    public void Reset_WhenInitialized_SetsIsInitializedToFalse()
    {
        // Arrange
        var shops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };
        CreateSut(shops);
        _sut.SelectShop();

        // Act
        _sut.Reset();

        // Assert
        _sut.IsInitialized.Should().BeFalse();
    }

    [Test]
    public void Reset_WhenNotInitialized_StaysNotInitialized()
    {
        // Arrange
        var shops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };
        CreateSut(shops);

        // Act
        _sut.Reset();

        // Assert
        _sut.IsInitialized.Should().BeFalse();
    }

    [Test]
    public void Reset_AfterSelectShop_ClearsInitialization()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);
        _sut.SelectShop("migros");
        _sut.IsInitialized.Should().BeTrue();

        // Act
        _sut.Reset();

        // Assert
        _sut.IsInitialized.Should().BeFalse();
    }

    [Test]
    public void Reset_PreservesSelectedShopData()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);
        _sut.SelectShop("migros");

        // Act
        _sut.Reset();

        // Assert
        _sut.SelectedShopKey.Should().Be("migros");
        _sut.SelectedShop.Should().Be(shops[1]);
    }

    [Test]
    public void Reset_CanReSelectShopAfterReset()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        CreateSut(shops);
        _sut.SelectShop("coop");
        _sut.Reset();

        // Act
        _sut.SelectShop("migros");

        // Assert
        _sut.SelectedShopKey.Should().Be("migros");
        _sut.IsInitialized.Should().BeTrue();
    }

    [Test]
    public void SelectShop_WithEmptyString_ThrowsInvalidOperationException()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig(string.Empty, "Empty", "https://empty.ch", "https://empty.ch/cart"),
        };
        CreateSut(shops);

        // Act
        var act = () => _sut.SelectShop("notmatching");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void SelectShop_WithEmptyStringKey_SelectsCorrectly()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig(string.Empty, "Empty", "https://empty.ch", "https://empty.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop(string.Empty);

        // Assert
        _sut.SelectedShopKey.Should().Be(string.Empty);
        _sut.SelectedShop!.Key.Should().Be(string.Empty);
    }

    [Test]
    public void SelectShop_WithMultipleShops_SelectsCorrectOne()
    {
        // Arrange
        var shops = new[]
        {
            new ShopConfig("shop1", "Shop 1", "https://shop1.ch", "https://shop1.ch/cart"),
            new ShopConfig("shop2", "Shop 2", "https://shop2.ch", "https://shop2.ch/cart"),
            new ShopConfig("shop3", "Shop 3", "https://shop3.ch", "https://shop3.ch/cart"),
            new ShopConfig("shop4", "Shop 4", "https://shop4.ch", "https://shop4.ch/cart"),
        };
        CreateSut(shops);

        // Act
        _sut.SelectShop("shop3");

        // Assert
        _sut.SelectedShopKey.Should().Be("shop3");
        _sut.SelectedShop.Should().Be(shops[2]);
    }

    [Test]
    public void SelectedShop_ReturnsCompleteShopConfig()
    {
        // Arrange
        var shop = new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart");
        CreateSut(new[] { shop });

        // Act
        _sut.SelectShop("coop");

        // Assert
        _sut.SelectedShop!.Key.Should().Be("coop");
        _sut.SelectedShop!.Name.Should().Be("Coop");
        _sut.SelectedShop!.BaseUrl.Should().Be("https://coop.ch");
        _sut.SelectedShop!.CartUrl.Should().Be("https://coop.ch/cart");
    }

    [Test]
    public void AvailableShops_ReflectsFactoryChanges()
    {
        // Arrange
        var initialShops = new[] { new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart") };
        CreateSut(initialShops);

        // Act
        var shops1 = _sut.AvailableShops;
        var newShops = new[]
        {
            new ShopConfig("coop", "Coop", "https://coop.ch", "https://coop.ch/cart"),
            new ShopConfig("migros", "Migros", "https://migros.ch", "https://migros.ch/cart"),
        };
        _factoryMock.AvailableShops.Returns(newShops);
        var shops2 = _sut.AvailableShops;

        // Assert
        shops1.Should().HaveCount(1);
        shops2.Should().HaveCount(2);
    }
}
