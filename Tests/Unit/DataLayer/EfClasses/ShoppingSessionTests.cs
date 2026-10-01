using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class ShoppingSessionTests
{
    [Test]
    public void CreateShoppingSession()
    {
        // Arrange
        var ingredientList = "500g carrots\n1L milk";
        var startedAt = DateTimeOffset.UtcNow;

        // Act
        var testee = new ShoppingSession(ingredientList, startedAt);

        // Assert
        testee.IngredientList.Should().Be(ingredientList);
        testee.StartedAt.Should().Be(startedAt);
    }

    [Test]
    public void DefaultConstructor_SetsIngredientListToEmptyString()
    {
        // Act — EF materializes instances via the protected parameterless constructor.
        var testee = (ShoppingSession)Activator.CreateInstance(typeof(ShoppingSession), nonPublic: true)!;

        // Assert
        testee.IngredientList.Should().Be(string.Empty);
    }

    [Test]
    public void CreateShoppingSession_DefaultsToInProgressWithoutCompletedAt()
    {
        // Arrange & Act
        var testee = new ShoppingSession("500g carrots", DateTimeOffset.UtcNow);

        // Assert
        testee.Status.Should().Be(SessionStatus.InProgress);
        testee.CompletedAt.Should().BeNull();
    }

    [Test]
    public void Complete_SetsStatusAndCompletedAtAtomically()
    {
        // Arrange — proves Status and CompletedAt can no longer be set independently and drift out of sync.
        var testee = new ShoppingSession("500g carrots", DateTimeOffset.UtcNow);
        var completedAt = DateTimeOffset.UtcNow.AddMinutes(10);

        // Act
        testee.Complete(completedAt);

        // Assert
        testee.Status.Should().Be(SessionStatus.Completed);
        testee.CompletedAt.Should().Be(completedAt);
    }
}
