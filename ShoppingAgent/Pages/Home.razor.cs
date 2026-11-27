using Microsoft.AspNetCore.Components;

namespace ShoppingAgent.Pages;

// Keep the component type resolvable when Stryker cannot run the Razor generator.
// https://github.com/stryker-mutator/stryker-net/issues/3813
#pragma warning disable MA0206 // Braces avoid SA1106 on the generator-independent empty partial declaration.
public sealed partial class Home : ComponentBase
{
}
#pragma warning restore MA0206
