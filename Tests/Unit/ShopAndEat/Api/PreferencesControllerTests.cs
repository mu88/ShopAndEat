using BizDbAccess;
using DataLayer.EfClasses;
using DTO.ShoppingPreference;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using ShopAndEat.Api;

namespace Tests.Unit.ShopAndEat.Api;

[TestFixture]
[Category("Unit")]
public class PreferencesControllerTests
{
    [Test]
    public async Task GetAll_ReturnsMappedPreferences()
    {
        // Arrange
        var preferencesRepositoryMock = Substitute.For<IPreferencesRepository>();
        var preference = new ShoppingPreference("global", "prefer_bio", PreferenceSource.UserConfirmed, null) { Value = "true" };
        preferencesRepositoryMock.GetAllPreferencesAsync("global", null).Returns([preference]);
        var testee = new PreferencesController(preferencesRepositoryMock, NullLogger<PreferencesController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.GetAll("global", cancellationToken: testee.HttpContext.RequestAborted);

        // Assert
        result.Value.Should().ContainSingle(response => response.Scope == "global" && response.Key == "prefer_bio" && response.Value == "true");
    }

    [Test]
    public async Task Upsert_SavesPreferenceWithRequestValues()
    {
        // Arrange
        var preferencesRepositoryMock = Substitute.For<IPreferencesRepository>();
        var testee = new PreferencesController(preferencesRepositoryMock, NullLogger<PreferencesController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        var request = new PreferenceRequest { Scope = "global", Key = "prefer_bio", Value = "true", Source = PreferenceSource.UserConfirmed, StoreKey = "coop" };

        // Act
        await testee.Upsert(request, testee.HttpContext.RequestAborted);

        // Assert
        await preferencesRepositoryMock.Received(1).UpsertPreferenceAsync(
            Arg.Is<ShoppingPreference>(preference =>
                preference.Scope == "global" && preference.Key == "prefer_bio" && preference.Value == "true" && preference.StoreKey == "coop"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Delete_WhenPreferenceExists_ReturnsNoContent()
    {
        // Arrange
        var preferencesRepositoryMock = Substitute.For<IPreferencesRepository>();
        preferencesRepositoryMock.DeletePreferenceAsync("global", "prefer_bio", null).Returns(true);
        var testee = new PreferencesController(preferencesRepositoryMock, NullLogger<PreferencesController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.Delete("global", "prefer_bio", cancellationToken: testee.HttpContext.RequestAborted);

        // Assert
        result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NoContent>();
    }

    [Test]
    public async Task Delete_WhenPreferenceDoesNotExist_ReturnsNotFoundProblem()
    {
        // Arrange
        var preferencesRepositoryMock = Substitute.For<IPreferencesRepository>();
        preferencesRepositoryMock.DeletePreferenceAsync("global", "prefer_bio", null).Returns(false);
        var testee = new PreferencesController(preferencesRepositoryMock, NullLogger<PreferencesController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        // Act
        var result = await testee.Delete("global", "prefer_bio", cancellationToken: testee.HttpContext.RequestAborted);

        // Assert
        var problem = result.Result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>().Subject;
        problem.ProblemDetails.Status.Should().Be(404);
        problem.ProblemDetails.Detail.Should().Be("No preference found for the specified scope, key, and store.");
    }
}
