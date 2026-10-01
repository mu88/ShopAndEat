#pragma warning disable SA1010 // Opening square brackets should not be preceded by a space

using FluentAssertions;
using Microsoft.Extensions.Localization;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Resources;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class SystemPromptBuilderTests
{
    private IPreferencesService _preferencesMock = null!;
    private ISessionService _sessionMock = null!;
    private IStringLocalizer<Messages> _localizerMock = null!;
    private SystemPromptBuilder _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _preferencesMock = Substitute.For<IPreferencesService>();
        _sessionMock = Substitute.For<ISessionService>();

        _localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        _localizerMock[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));

        _sut = new SystemPromptBuilder(_preferencesMock, _sessionMock, _localizerMock);
    }

    [Test]
    public async Task BuildSystemPromptAsync_IncludesShopName()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        result.Should().Contain("Coop");
    }

    [Test]
    public async Task BuildSystemPromptAsync_IncludesShopUrl()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        result.Should().Contain("https://www.coop.ch");
    }

    [Test]
    public async Task BuildSystemPromptAsync_IncludesUnits()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg", "g", "l"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        result.Should().Contain("\"kg\"");
        result.Should().Contain("\"g\"");
        result.Should().Contain("\"l\"");
    }

    [Test]
    public async Task BuildSystemPromptAsync_WithPreferences_IncludesPreferences()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new() { Scope = "global", Key = "prefer_bio", Value = "true" },
                new() { Scope = "article:Milk", Key = "confirmed_product", Value = "Bio Milk 1L" },
            ]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        result.Should().Contain("LearnedPreferences");
        result.Should().Contain("[global] prefer_bio: `true`");
        result.Should().Contain("[article:Milk] confirmed_product: `Bio Milk 1L`");
    }

    [Test]
    public async Task BuildSystemPromptAsync_WithEmptyPreferences_DoesNotIncludePreferenceSection()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        result.Should().NotContain("LearnedPreferences");
    }

    [Test]
    public async Task BuildSystemPromptAsync_ContainsAlwaysAskInstructions()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        result.Should().Contain("always_ask");
    }

    [Test]
    public async Task BuildSystemPromptAsync_DoesNotContainHardCodedFreshProduceRule()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert — these were hard-coded defaults that are now removed; user preferences drive this behavior
        result.Should().NotContain("Prefer fresh produce over canned");
        result.Should().NotContain("Do not choose beverages, sauces, or prepared foods when searching for raw ingredients");
    }

    [Test]
    public async Task BuildSystemPromptAsync_WithEmptyUnits_ProducesEmptyUnitList()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert — with no units the placeholder is replaced by an empty string
        result.Should().Contain("do NOT belong in the search term: ")
            .And.NotContain("{unitList}");
    }

    [Test]
    public async Task BuildSystemPromptAsync_PreferenceValueContainsCarriageReturn_StripsCarriageReturnNotReplace()
    {
        // Arrange — Value.Replace('\n', ' ').Replace("\r", string.Empty) must strip "\r" entirely,
        // not substitute it with anything.
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new() { Scope = "global", Key = "note", Value = "line1\r\nline2" }]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        result.Should().Contain("[global] note: `line1 line2`");
    }

    [Test]
    public async Task BuildSystemPromptAsync_WithEmptyPreferences_ProducesExactlyTheTemplateWithoutPreferenceText()
    {
        // Arrange — proves prefText is truly the empty string (not some other placeholder value) when
        // there are no preferences, by comparing against the raw embedded template with all other
        // placeholders substituted and {prefText} removed.
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        var expected = LoadEmbeddedTemplate()
            .Replace("{shopName}", "Coop", StringComparison.Ordinal)
            .Replace("{shopUrl}", "https://www.coop.ch", StringComparison.Ordinal)
            .Replace("{unitList}", "\"kg\"", StringComparison.Ordinal)
            .Replace("{prefText}", string.Empty, StringComparison.Ordinal);
        result.Should().Be(expected);
    }

    [Test]
    public async Task BuildSystemPromptAsync_WithMultipleUnits_JoinsWithCommaAndSpace()
    {
        // Arrange
        _preferencesMock.GetAllPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _sessionMock.GetUnitsAsync(Arg.Any<CancellationToken>())
            .Returns(["kg", "g", "l"]);

        // Act
        var result = await _sut.BuildSystemPromptAsync("Coop", "https://www.coop.ch", "coop");

        // Assert
        result.Should().Contain("\"kg\", \"g\", \"l\"");
    }

    private static string LoadEmbeddedTemplate()
    {
        var assembly = typeof(SystemPromptBuilder).Assembly;
        using var stream = assembly.GetManifestResourceStream("ShoppingAgent.Resources.SystemPrompt.txt")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
