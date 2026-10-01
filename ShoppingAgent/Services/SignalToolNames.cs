namespace ShoppingAgent.Services;

/// <summary>
/// Canonical names of the "signal" tools that drive shopping-workflow phase transitions
/// (as opposed to regular data/action tools). Centralized here to avoid magic-string
/// duplication across the tool dispatch, rendering, and definition layers.
/// </summary>
public static class SignalToolNames
{
    public const string ConfirmCart = "confirm_cart";
    public const string ProceedToCart = "proceed_to_cart";
}
