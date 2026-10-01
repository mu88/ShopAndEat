using System.Reflection;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;

namespace Tests.Unit.ServiceLayer;

/// <summary>
/// Guard tests for <see cref="SimpleCrudHelper"/>'s hand-written, per-key-type <c>FindAsync</c>/
/// <c>DeleteAsync</c> overloads (needed because <see cref="IHasId{TId}"/> is a marker interface with
/// no members, so the key type can't be obtained generically). Without these, adding a new
/// <see cref="IHasId{TId}"/> entity and forgetting its <c>FindAsync</c> overload - or adding a
/// <c>DeleteAsync</c> overload for a key type nobody can even look up - would compile silently and
/// only surface as a runtime <see cref="System.InvalidOperationException"/>/wrong-overload-resolution
/// surprise much later.
/// </summary>
[TestFixture]
[Category("Unit")]
public class SimpleCrudHelperConsistencyTests
{
    [Test]
    public void FindAsync_HasAnOverloadForEveryIHasIdEntitysKeyType()
    {
        // Arrange
        var entityKeyTypes = GetEntityKeyTypes();
        var findAsyncKeyTypes = GetSimpleCrudHelperKeyParameterTypes(nameof(SimpleCrudHelper.FindAsync));

        // Assert
        entityKeyTypes.Should().BeSubsetOf(
            findAsyncKeyTypes,
            "every IHasId<TId> entity's key type must have a matching SimpleCrudHelper.FindAsync overload, otherwise that entity type can never be looked up");
    }

    [Test]
    public void DeleteAsync_OnlyHasOverloadsForKeyTypesThatAlsoHaveAFindAsyncOverload()
    {
        // Arrange
        var deleteAsyncKeyTypes = GetSimpleCrudHelperKeyParameterTypes(nameof(SimpleCrudHelper.DeleteAsync));
        var findAsyncKeyTypes = GetSimpleCrudHelperKeyParameterTypes(nameof(SimpleCrudHelper.FindAsync));

        // Assert
        deleteAsyncKeyTypes.Should().BeSubsetOf(
            findAsyncKeyTypes,
            "a DeleteAsync overload for a key type with no matching FindAsync overload would let callers delete an entity type they can never look up - likely a copy/paste oversight");
    }

    private static List<Type> GetEntityKeyTypes() =>
        typeof(IHasId<>).Assembly
            .GetTypes()
            .SelectMany(type => type.GetInterfaces())
            .Where(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IHasId<>))
            .Select(interfaceType => interfaceType.GetGenericArguments()[0])
            .Distinct()
            .ToList();

    private static List<Type> GetSimpleCrudHelperKeyParameterTypes(string methodName) =>
        typeof(SimpleCrudHelper)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
            .Select(method => method.GetParameters()[0].ParameterType)
            .Distinct()
            .ToList();
}
