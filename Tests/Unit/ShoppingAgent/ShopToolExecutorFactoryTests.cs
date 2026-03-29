using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Models;
using ShoppingAgent.Options;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ShopToolExecutorFactoryTests
{
    private static IOptions<ShopOptions> CreateShopOptions() =>
        Options.Create(new ShopOptions
        {
            Shops =
            [
                new("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart"),
            ],
        });

    [Test]
    public void AvailableShops_ReturnsCoopShop()
    {
        // Arrange
        var bridgeMock = Substitute.For<IExtensionBridge>();
        var loggerFactoryMock = Substitute.For<ILoggerFactory>();
        var testee = new ShopToolExecutorFactory(bridgeMock, loggerFactoryMock, CreateShopOptions());

        // Act
        var shops = testee.AvailableShops;

        // Assert
        shops.Should().ContainSingle();
        shops[0].Key.Should().Be("coop");
    }

    [Test]
    public void GetExecutor_WithCoop_ReturnsCoopToolExecutor()
    {
        // Arrange
        var bridgeMock = Substitute.For<IExtensionBridge>();
        var loggerFactoryMock = Substitute.For<ILoggerFactory>();
        loggerFactoryMock.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var testee = new ShopToolExecutorFactory(bridgeMock, loggerFactoryMock, CreateShopOptions());

        // Act
        var executor = testee.GetExecutor("coop");

        // Assert
        executor.Should().BeOfType<CoopToolExecutor>();
    }

    [Test]
    public void GetExecutor_WithUnknownShop_ThrowsArgumentException()
    {
        // Arrange
        var bridgeMock = Substitute.For<IExtensionBridge>();
        var loggerFactoryMock = Substitute.For<ILoggerFactory>();
        var testee = new ShopToolExecutorFactory(bridgeMock, loggerFactoryMock, CreateShopOptions());

        // Act
        var act = () => testee.GetExecutor("unknown");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown shop*unknown*");
    }
}
