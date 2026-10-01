using DTO.ShoppingSession;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.Unit.DTO;

[TestFixture]
[Category("Unit")]
public class SessionDtosTests
{
    [Test]
    public void CreateSessionRequest_DefaultsIngredientListToEmptyString()
    {
        new CreateSessionRequest().IngredientList.Should().Be(string.Empty);
    }

    [Test]
    public void AddSessionItemRequest_DefaultsStringPropertiesToEmptyString()
    {
        var testee = new AddSessionItemRequest();

        testee.OriginalIngredient.Should().Be(string.Empty);
        testee.SelectedProductName.Should().Be(string.Empty);
        testee.SelectedProductUrl.Should().Be(string.Empty);
        testee.Price.Should().Be(string.Empty);
    }

    [Test]
    public void SessionResponse_DefaultsStringPropertiesToEmptyString()
    {
        var testee = new SessionResponse();

        testee.Status.Should().Be(string.Empty);
        testee.IngredientList.Should().Be(string.Empty);
    }

    [Test]
    public void SessionDetailResponse_DefaultsStringPropertiesToEmptyString()
    {
        var testee = new SessionDetailResponse();

        testee.Status.Should().Be(string.Empty);
        testee.IngredientList.Should().Be(string.Empty);
    }

    [Test]
    public void SessionItemResponse_DefaultsStringPropertiesToEmptyString()
    {
        var testee = new SessionItemResponse();

        testee.OriginalIngredient.Should().Be(string.Empty);
        testee.SelectedProductName.Should().Be(string.Empty);
        testee.SelectedProductUrl.Should().Be(string.Empty);
        testee.Price.Should().Be(string.Empty);
        testee.Status.Should().Be(string.Empty);
    }
}
