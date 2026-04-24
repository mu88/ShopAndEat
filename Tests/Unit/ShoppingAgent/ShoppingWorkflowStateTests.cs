using FluentAssertions;
using NUnit.Framework;
using ShoppingAgent.Models;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ShoppingWorkflowStateTests
{
    private ShoppingWorkflowState _sut;

    [SetUp]
    public void SetUp()
    {
        _sut = new ShoppingWorkflowState();
    }

    [Test]
    public void InitialPhase_ShouldBeResearching()
    {
        // Arrange & Act & Assert
        _sut.Phase.Should().Be(WorkflowPhase.Researching);
    }

    [Test]
    public void MoveToAwaitingConfirmation_FromResearching_SetsPhase()
    {
        // Arrange — phase starts at Researching

        // Act
        _sut.MoveToAwaitingConfirmation();

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.AwaitingConfirmation);
    }

    [Test]
    public void MoveToAwaitingConfirmation_FromFillingCart_SetsPhase()
    {
        // Arrange
        _sut.MoveToAwaitingConfirmation();
        _sut.MoveToFillingCart();

        // Act
        _sut.MoveToAwaitingConfirmation();

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.AwaitingConfirmation);
    }

    [Test]
    public void MoveToAwaitingConfirmation_FromAwaitingConfirmation_SetsPhase()
    {
        // Arrange
        _sut.MoveToAwaitingConfirmation();

        // Act
        _sut.MoveToAwaitingConfirmation();

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.AwaitingConfirmation);
    }

    [Test]
    public void MoveToFillingCart_FromAwaitingConfirmation_SetsPhase()
    {
        // Arrange
        _sut.MoveToAwaitingConfirmation();

        // Act
        _sut.MoveToFillingCart();

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.FillingCart);
    }

    [Test]
    public void MoveToFillingCart_FromResearching_ThrowsInvalidOperationException()
    {
        // Arrange — phase starts at Researching

        // Act
        var act = () => _sut.MoveToFillingCart();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void MoveToFillingCart_FromFillingCart_ThrowsInvalidOperationException()
    {
        // Arrange
        _sut.MoveToAwaitingConfirmation();
        _sut.MoveToFillingCart();

        // Act
        var act = () => _sut.MoveToFillingCart();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Reset_FromResearching_StaysAtResearching()
    {
        // Arrange — phase starts at Researching

        // Act
        _sut.Reset();

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.Researching);
    }

    [Test]
    public void Reset_FromAwaitingConfirmation_SetsResearching()
    {
        // Arrange
        _sut.MoveToAwaitingConfirmation();

        // Act
        _sut.Reset();

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.Researching);
    }

    [Test]
    public void MoveToAwaitingClarification_FromResearching_SetsPhaseAndStoresPendingItems()
    {
        // Arrange
        var items = new[] { "Garlic", "Lemon", "Breakfast fruit" };

        // Act
        _sut.MoveToAwaitingClarification(items);

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.AwaitingClarification);
        _sut.PendingItems.Should().BeEquivalentTo(items);
    }

    [Test]
    public void MoveToAwaitingClarification_FromAwaitingConfirmation_SetsPhaseAndStoresPendingItems()
    {
        // Arrange
        _sut.MoveToAwaitingConfirmation();

        // Act
        _sut.MoveToAwaitingClarification(["Lemon"]);

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.AwaitingClarification);
        _sut.PendingItems.Should().ContainSingle().Which.Should().Be("Lemon");
    }

    [Test]
    public void MoveToAwaitingClarification_FromFillingCart_ThrowsInvalidOperationException()
    {
        // Arrange
        _sut.MoveToAwaitingConfirmation();
        _sut.MoveToFillingCart();

        // Act
        var act = () => _sut.MoveToAwaitingClarification(["Garlic"]);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Reset_ClearsPendingItems()
    {
        // Arrange
        _sut.MoveToAwaitingClarification(["Garlic", "Lemon"]);

        // Act
        _sut.Reset();

        // Assert
        _sut.PendingItems.Should().BeEmpty();
        _sut.Phase.Should().Be(WorkflowPhase.Researching);
    }

    [Test]
    public void PendingItems_InitiallyEmpty()
    {
        // Arrange & Act & Assert
        _sut.PendingItems.Should().BeEmpty();
    }

    [Test]
    public void Reset_FromFillingCart_SetsResearching()
    {
        // Arrange
        _sut.MoveToAwaitingConfirmation();
        _sut.MoveToFillingCart();

        // Act
        _sut.Reset();

        // Assert
        _sut.Phase.Should().Be(WorkflowPhase.Researching);
    }
}