namespace ShoppingAgent.Models;

/// <summary>
/// Represents the current phase of the two-phase shopping workflow.
/// </summary>
public enum WorkflowPhase
{
    /// <summary>LLM is researching products and building the shopping plan.</summary>
    Researching,

    /// <summary>LLM has identified unresolved items and is waiting for user clarification.</summary>
    AwaitingClarification,

    /// <summary>LLM has presented the plan and is waiting for user confirmation.</summary>
    AwaitingConfirmation,

    /// <summary>User confirmed — LLM is adding products to the cart.</summary>
    FillingCart,
}
