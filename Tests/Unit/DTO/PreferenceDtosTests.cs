using DTO.ShoppingPreference;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DTO;

[TestFixture]
[Category("Unit")]
public class PreferenceDtosTests
{
    [Test]
    public void PreferenceRequest_DefaultsStringPropertiesToEmptyString()
    {
        var testee = new PreferenceRequest();

        testee.Scope.Should().Be(string.Empty);
        testee.Key.Should().Be(string.Empty);
        testee.Value.Should().Be(string.Empty);
    }

    [Test]
    public void PreferenceResponse_DefaultsStringPropertiesToEmptyString()
    {
        var testee = new PreferenceResponse();

        testee.Scope.Should().Be(string.Empty);
        testee.Key.Should().Be(string.Empty);
        testee.Value.Should().Be(string.Empty);
        testee.Source.Should().Be(string.Empty);
    }
}
