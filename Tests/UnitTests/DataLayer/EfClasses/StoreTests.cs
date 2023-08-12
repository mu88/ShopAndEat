using System;
using System.Collections.ObjectModel;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses;

[TestFixture]
[Category("Unit")]
public class StoreTests
{
    [Test]
    public void CreateStore()
    {
        var name = "London";
        var compartments = new Collection<ShoppingOrder> { new ShoppingOrder(new ArticleGroup("Vegetables"), 30) };

        var testee = new Store(name, compartments);

        testee.Name.Should().Be(name);
        testee.Compartments.Should().BeEquivalentTo(compartments);
    }

    [Test]
    public void AddCompartment()
    {
        var name = "London";
        var existingCompartment = new ShoppingOrder(new ArticleGroup("Vegetables"), 30);
        var compartments = new Collection<ShoppingOrder> { existingCompartment };
        var compartmentToAdd = new ShoppingOrder(new ArticleGroup("Vegetables"), 40);
        var testee = new Store(name, compartments);

        testee.AddCompartment(compartmentToAdd);

        testee.Compartments.Should().BeEquivalentTo(new []{existingCompartment, compartmentToAdd});
    }

    [Test]
    public void AddingCompartmentWithSameOrderThrowsException()
    {
        var name = "London";
        var existingCompartment = new ShoppingOrder(new ArticleGroup("Vegetables"), 30);
        var compartments = new Collection<ShoppingOrder> { existingCompartment };
        var compartmentToAdd = new ShoppingOrder(new ArticleGroup("Vegetables"), 30);
        var testee = new Store(name, compartments);

        testee.Invoking(x => x.AddCompartment(compartmentToAdd)).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void DeleteCompartment()
    {
        var name = "London";
        var existingCompartment = new ShoppingOrder(new ArticleGroup("Vegetables"), 30);
        var compartments = new Collection<ShoppingOrder> { existingCompartment };
        var testee = new Store(name, compartments);

        testee.DeleteCompartment(existingCompartment);

        testee.Compartments.Should().BeEmpty();
    }
}