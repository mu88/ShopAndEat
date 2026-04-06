namespace ShoppingAgent.Resources;

/// <summary>
/// Marker class for <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/>.
/// Resource files: Messages.resx (en), Messages.de.resx (de).
/// </summary>
/// <remarks>
/// The generic type parameter of <c>IStringLocalizer&lt;T&gt;</c> serves two purposes:
/// (1) it pins the lookup to the assembly where T is defined, and
/// (2) its namespace determines the resource path (ShoppingAgent/Resources/Messages.resx).
/// Without this class, injecting <c>IStringLocalizer&lt;Messages&gt;</c> from within
/// ShoppingAgent would not resolve the correct .resx files.
/// </remarks>
public class Messages { }
