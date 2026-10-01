namespace DataLayer.EfClasses;

/// <summary>
/// Marker interface tying an EF Core entity to its own primary-key type, so that <c>SimpleCrudHelper</c>'s
/// generic Find/Delete overloads can be constrained at compile time to the entity's actual key type —
/// e.g. preventing <c>FindAsync&lt;Store&gt;(someArticleId)</c> or <c>FindAsync&lt;Store&gt;(5)</c> from
/// compiling against the wrong key type.
/// </summary>
/// <typeparam name="TId">The type of this entity's primary key.</typeparam>
// S2326: TId is intentionally unused in the interface body — it exists purely so SimpleCrudHelper's
// generic constraints can bind a type parameter to its matching key type; the interface itself has
// no members by design.
#pragma warning disable S2326
public interface IHasId<TId>
#pragma warning restore S2326
{
}
