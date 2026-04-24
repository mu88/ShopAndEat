using ShoppingAgent.Models;

namespace ShoppingAgent.Services.Concrete;

/// <inheritdoc />
public sealed class ShoppingWorkflowState : IShoppingWorkflowState
{
    private List<string> _pendingItems = [];

    public WorkflowPhase Phase { get; private set; } = WorkflowPhase.Researching;

    public IReadOnlyList<string> PendingItems => _pendingItems;

    public void MoveToAwaitingClarification(IEnumerable<string> pendingItems)
    {
        if (Phase == WorkflowPhase.FillingCart)
        {
            throw new InvalidOperationException(
                $"Cannot move to {nameof(WorkflowPhase.AwaitingClarification)} from {nameof(WorkflowPhase.FillingCart)}.");
        }

        _pendingItems = pendingItems.ToList();
        Phase = WorkflowPhase.AwaitingClarification;
    }

    public void MoveToAwaitingConfirmation() =>
        Phase = WorkflowPhase.AwaitingConfirmation;

    public void MoveToFillingCart()
    {
        if (Phase != WorkflowPhase.AwaitingConfirmation)
        {
            throw new InvalidOperationException(
                $"Cannot move to {nameof(WorkflowPhase.FillingCart)} from {Phase}. Must be in {nameof(WorkflowPhase.AwaitingConfirmation)} first.");
        }

        Phase = WorkflowPhase.FillingCart;
    }

    public void Reset()
    {
        _pendingItems = [];
        Phase = WorkflowPhase.Researching;
    }
}
