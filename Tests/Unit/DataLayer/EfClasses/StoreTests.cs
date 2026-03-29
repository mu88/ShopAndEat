using System.Collections.ObjectModel;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class StoreTests
{
    [Test]
    public void CreateStore()
    {
        // Arrange
        var name = "London";
        var compartments = new Collection<ShoppingOrder> { new(new ArticleGroup("Vegetables"), 30) };

        // Act
        var testee = new Store(name, compartments);

        // Assert
        testee.Name.Should().Be(name);
        testee.Compartments.Should().BeEquivalentTo(compartments);
    }

    [Test]
    public void AddCompartment()
    {
        // Arrange
        var name = "London";
        var existingCompartment = new ShoppingOrder(new ArticleGroup("Vegetables"), 30);
        var compartments = new Collection<ShoppingOrder> { existingCompartment };
        var compartmentToAdd = new ShoppingOrder(new ArticleGroup("Vegetables"), 40);
        var testee = new Store(name, compartments);

        // Act
        testee.AddCompartment(compartmentToAdd);

        // Assert
        testee.Compartments.Should().BeEquivalentTo([existingCompartment, compartmentToAdd]);
    }

    [Test]
    public void AddingCompartmentWithSameOrderThrowsException()
    {
        // Arrange
        var name = "London";
        var existingCompartment = new ShoppingOrder(new ArticleGroup("Vegetables"), 30);
        var compartments = new Collection<ShoppingOrder> { existingCompartment };
        var compartmentToAdd = new ShoppingOrder(new ArticleGroup("Vegetables"), 30);
        var testee = new Store(name, compartments);

        // Act & Assert
        testee.Invoking(x => x.AddCompartment(compartmentToAdd)).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void DeleteCompartment()
    {
        // Arrange
        var name = "London";
        var existingCompartment = new ShoppingOrder(new ArticleGroup("Vegetables"), 30);
        var compartments = new Collection<ShoppingOrder> { existingCompartment };
        var testee = new Store(name, compartments);

        // Act
        testee.DeleteCompartment(existingCompartment);

        // Assert
        testee.Compartments.Should().BeEmpty();
    }
}
