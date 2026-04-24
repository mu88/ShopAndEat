using ShoppingAgent.Models;

namespace ShoppingAgent.Services;

/// <summary>
/// Tracks the current phase of the two-phase shopping workflow and provides
/// guarded transitions between phases. Registered as Scoped so each Blazor
/// circuit (user session) gets its own instance.
/// </summary>
public interface IShoppingWorkflowState
{
    WorkflowPhase Phase { get; }

    /// <summary>Items that are unresolved and require user input before the plan can be confirmed.</summary>
    IReadOnlyList<string> PendingItems { get; }

    /// <summary>Transitions to <see cref="WorkflowPhase.AwaitingClarification"/> with the given pending items. Valid from any phase except <see cref="WorkflowPhase.FillingCart"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when called from <see cref="WorkflowPhase.FillingCart"/>.</exception>
    void MoveToAwaitingClarification(IEnumerable<string> pendingItems);

    /// <summary>Transitions to <see cref="WorkflowPhase.AwaitingConfirmation"/>. Valid from any phase.</summary>
    void MoveToAwaitingConfirmation();

    /// <summary>Transitions to <see cref="WorkflowPhase.FillingCart"/>. Only valid from <see cref="WorkflowPhase.AwaitingConfirmation"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when called from a phase other than <see cref="WorkflowPhase.AwaitingConfirmation"/>.</exception>
    void MoveToFillingCart();

    /// <summary>Resets to <see cref="WorkflowPhase.Researching"/> and clears pending items. Always valid.</summary>
    void Reset();
}
