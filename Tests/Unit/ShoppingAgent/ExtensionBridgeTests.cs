using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Models;
using ShoppingAgent.Options;
using ShoppingAgent.Resources;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class ExtensionBridgeTests
{
    private FakeJSRuntime _fakeJs;
    private IStringLocalizer<Messages> _localizerMock;

    [SetUp]
    public void SetUp()
    {
        _fakeJs = new FakeJSRuntime();

        _localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        _localizerMock[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));
    }

    [Test]
    public void IsExtensionConnected_ReturnsFalse_Initially()
    {
        // Act
        var testee = CreateTestee();

        // Assert
        testee.IsExtensionConnected.Should().BeFalse();
    }

    [Test]
    public void OnExtensionConnected_SetsIsConnectedTrue()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        testee.OnExtensionConnected();

        // Assert
        testee.IsExtensionConnected.Should().BeTrue();
    }

    [Test]
    public void OnExtensionDisconnected_SetsIsConnectedFalse()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        // Act
        testee.OnExtensionDisconnected();

        // Assert
        testee.IsExtensionConnected.Should().BeFalse();
    }

    [Test]
    public async Task ExecuteToolAsync_ReturnsError_WhenNotConnected()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var result = await testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task ExecuteToolAsync_ReturnsResult_WhenToolCompletes()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        // Act
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal) { ["term"] = "Tofu" }, "coop");

        // The callId was captured synchronously when InvokeAsync ran during ExecuteToolAsync
        _fakeJs.LastSendToolCallJson.Should().NotBeNullOrEmpty();
        var doc = JsonDocument.Parse(_fakeJs.LastSendToolCallJson);
        var callId = doc.RootElement.GetProperty("id").GetString();

        var resultJson = JsonSerializer.Serialize(new ToolResult
        {
            Success = true,
            Data = "[{\"Name\":\"Organic Tofu\"}]",
            Id = callId,
        });
        testee.OnToolResult(resultJson);

        var result = await executeTask;

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Contain("Organic Tofu");
    }

    [Test]
    public async Task ExecuteToolAsync_ReturnsTimeout_WhenCancellationTokenPreCancelled()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop", cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void OnConnectionChanged_IsFired_WhenConnectionStateChanges()
    {
        // Arrange
        var testee = CreateTestee();
        var eventFired = 0;
        testee.OnConnectionChanged += () => eventFired++;

        // Act
        testee.OnExtensionConnected();
        testee.OnExtensionDisconnected();

        // Assert
        eventFired.Should().Be(2);
    }

    [Test]
    public void OnToolResult_DoesNotThrow_WhenResultJsonIsInvalid()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var act = () => testee.OnToolResult("not-valid-json{{{");

        // Assert
        act.Should().NotThrow();
    }

    private ExtensionBridge CreateTestee() => new(_fakeJs, _localizerMock, NullLogger<ExtensionBridge>.Instance, Options.Create(new ExtensionOptions()));

    private sealed class FakeJSRuntime : IJSRuntime
    {
        public string LastSendToolCallJson { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            if (string.Equals(identifier, "extensionBridge.sendToolCall", StringComparison.Ordinal) && args?.Length > 0)
            {
                LastSendToolCallJson = args[0] as string;
            }

            return new ValueTask<TValue>(default(TValue));
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
            => InvokeAsync<TValue>(identifier, args);
    }
}
