namespace ShopAndEat.Api.Resources;

/// <summary>
/// Marker class for <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/>.
/// Resource files: Messages.resx (en), Messages.de.resx (de).
/// </summary>
/// <remarks>
/// The generic type parameter of <c>IStringLocalizer&lt;T&gt;</c> serves two purposes:
/// (1) it pins the lookup to the assembly where T is defined, and
/// (2) its namespace determines the resource path (ShopAndEat/Api/Resources/Messages.resx).
/// </remarks>
#pragma warning disable SA1106 // StyleCop misidentifies the C# 12 semicolon type-body as an empty statement
public class Messages;
#pragma warning restore SA1106
