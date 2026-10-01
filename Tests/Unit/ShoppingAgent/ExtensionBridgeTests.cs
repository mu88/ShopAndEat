using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using NSubstitute;
using NUnit.Framework;
using ShoppingAgent.Diagnostics;
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
    private readonly List<Activity> _completedActivities = [];
    private FakeJSRuntime _fakeJs = null!;
    private IStringLocalizer<Messages> _localizerMock = null!;
    private ActivityListener _activityListener = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeJs = new FakeJSRuntime();

        _localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        _localizerMock[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));

        _completedActivities.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, ShoppingAgentDiagnostics.ActivitySourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _completedActivities.Add,
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TearDown]
    public void TearDown() => _activityListener.Dispose();

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
        result.Error.Should().Be("ExtensionNotConnectedError");
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
            Id = callId!,
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
        result.Error.Should().Be("ToolCallTimeout");
    }

    [Test]
    public async Task ExecuteToolAsync_SetsTimeoutErrorStatusOnActivity_WhenCallNeverResolves()
    {
        // Arrange: ToolCallTimeoutSeconds=1 keeps this deterministic and fast instead of hanging for the real 30s default.
        var testee = CreateTestee(toolCallTimeoutSeconds: 1);
        testee.OnExtensionConnected();

        // Act
        var result = await testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop")
            .WaitAsync(TimeSpan.FromSeconds(3));

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("ToolCallTimeout");
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("timeout");
    }

    [Test]
    public async Task ExecuteToolAsync_RecordsActivityTags_WhenToolCompletesSuccessfully()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        // Act
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal) { ["term"] = "Tofu" }, "coop");
        var callId = ExtractCallId(_fakeJs.LastSendToolCallJson);
        testee.OnToolResult(JsonSerializer.Serialize(new ToolResult { Success = true, Data = "Organic Tofu", Id = callId }));
        await executeTask;

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.OperationName.Should().Be("ShoppingAgent.ExtensionBridge.Invoke");
        activity.GetTagItem("extension.tool").Should().Be("search");
        activity.GetTagItem("extension.shop").Should().Be("coop");
        activity.GetTagItem("extension.call_id").Should().Be(callId);
        activity.GetTagItem("extension.success").Should().Be(true);
        activity.Status.Should().Be(ActivityStatusCode.Unset);
    }

    [Test]
    public async Task ExecuteToolAsync_SetsErrorStatusOnActivity_WhenToolReportsFailure()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        // Act
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop");
        var callId = ExtractCallId(_fakeJs.LastSendToolCallJson);
        testee.OnToolResult(JsonSerializer.Serialize(new ToolResult { Success = false, Error = "Product not found", Id = callId }));
        await executeTask;

        // Assert
        var activity = _completedActivities.Should().ContainSingle().Subject;
        activity.GetTagItem("extension.success").Should().Be(false);
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Product not found");
    }

    [Test]
    public async Task ExecuteToolAsync_ReturnsResult_WhenNoActivityListenerIsRegistered_AndToolCompletesSuccessfully()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetTag` branches used for optional OpenTelemetry tagging.
        _activityListener.Dispose();
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        // Act
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop");
        var callId = ExtractCallId(_fakeJs.LastSendToolCallJson);
        testee.OnToolResult(JsonSerializer.Serialize(new ToolResult { Success = true, Data = "Organic Tofu", Id = callId }));
        var result = await executeTask;

        // Assert
        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task ExecuteToolAsync_ReturnsResult_WhenNoActivityListenerIsRegistered_AndToolReportsFailure()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetStatus` branch used for optional OpenTelemetry error tagging.
        _activityListener.Dispose();
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        // Act
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop");
        var callId = ExtractCallId(_fakeJs.LastSendToolCallJson);
        testee.OnToolResult(JsonSerializer.Serialize(new ToolResult { Success = false, Error = "Product not found", Id = callId }));
        var result = await executeTask;

        // Assert
        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task ExecuteToolAsync_ReturnsTimeout_WhenNoActivityListenerIsRegistered_AndCallNeverResolves()
    {
        // Arrange — without a listener, ActivitySource.StartActivity returns null, exercising
        // the null-conditional `activity?.SetStatus` branch in the timeout/catch path.
        _activityListener.Dispose();
        var testee = CreateTestee(toolCallTimeoutSeconds: 1);
        testee.OnExtensionConnected();

        // Act
        var result = await testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop")
            .WaitAsync(TimeSpan.FromSeconds(3));

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("ToolCallTimeout");
    }

    [Test]
    public async Task OnToolResult_DeserializesCaseInsensitively_WhenJsonKeysUseDifferentCasing()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        // Act
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop");
        var callId = ExtractCallId(_fakeJs.LastSendToolCallJson);
        testee.OnToolResult($$"""{"success":true,"data":"lowercase-worked","id":"{{callId}}"}""");
        var result = await executeTask;

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be("lowercase-worked");
    }

    [Test]
    public async Task InitializeAsync_SendsDotNetReferenceToJs()
    {
        // Act
        await CreateTestee().InitializeAsync();

        // Assert
        var invocation = _fakeJs.Invocations.Should().ContainSingle(i => string.Equals(i.Identifier, "extensionBridge.initialize", StringComparison.Ordinal)).Subject;
        invocation.Args.Should().ContainSingle().Which.Should().BeOfType<DotNetObjectReference<ExtensionBridge>>();
    }

    [Test]
    public async Task InitializeAsync_DisposesPreviousDotNetReference_WhenCalledTwice()
    {
        // Arrange
        var testee = CreateTestee();
        await testee.InitializeAsync();
        var firstReference = (DotNetObjectReference<ExtensionBridge>)_fakeJs.Invocations
            .Single(i => string.Equals(i.Identifier, "extensionBridge.initialize", StringComparison.Ordinal)).Args[0] !;

        // Act
        await testee.InitializeAsync();

        // Assert: DotNetObjectReference.Value throws once the reference has been disposed.
        var act = () => firstReference.Value;
        act.Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public async Task InitializeAsync_CallsJsInitializeTwice_WhenCalledTwice_EnsuresIdempotentJsHandling()
    {
        // Arrange — Verify that repeated InitializeAsync calls result in repeated JS initialize calls.
        // This ensures that the JS side's idempotent initialization handling (removing old listeners)
        // is triggered each time, preventing event listener leaks when the Blazor component is
        // re-initialized after navigation.
        var testee = CreateTestee();

        // Act
        await testee.InitializeAsync();
        await testee.InitializeAsync();

        // Assert — both calls should invoke extensionBridge.initialize via JS
        var initializeInvocations = _fakeJs.Invocations
            .Where(i => string.Equals(i.Identifier, "extensionBridge.initialize", StringComparison.Ordinal))
            .ToList();
        initializeInvocations.Should().HaveCount(2);
    }

    [Test]
    public async Task DisposeAsync_CallsJsDispose_WhenInitialized()
    {
        // Arrange
        var testee = CreateTestee();
        await testee.InitializeAsync();

        // Act
        await testee.DisposeAsync();

        // Assert
        _fakeJs.Invocations.Should().Contain(i => i.Identifier == "extensionBridge.dispose");
    }

    [Test]
    public async Task DisposeAsync_IsIdempotent_WhenCalledTwice()
    {
#pragma warning disable IDISP016 // intentionally re-disposing to verify idempotency
        // Arrange — IExtensionBridge is a scoped DI service; nothing prevents both the DI container
        // and some other code path from disposing it, so a second call must be a safe no-op.
        var testee = CreateTestee();
        await testee.InitializeAsync();

        // Act
        await testee.DisposeAsync();
        var secondDispose = async () =>
        {
            await testee.DisposeAsync();
        };

        // Assert
        await secondDispose.Should().NotThrowAsync();
        _fakeJs.Invocations.Count(i => string.Equals(i.Identifier, "extensionBridge.dispose", StringComparison.Ordinal)).Should().Be(1);
#pragma warning restore IDISP016
    }

    [Test]
    public async Task DisposeAsync_DisposesDotNetReference_WhenInitialized()
    {
        // Arrange
        var testee = CreateTestee();
        await testee.InitializeAsync();
        var reference = (DotNetObjectReference<ExtensionBridge>)_fakeJs.Invocations
            .Single(i => string.Equals(i.Identifier, "extensionBridge.initialize", StringComparison.Ordinal)).Args[0] !;

        // Act
        await testee.DisposeAsync();

        // Assert: DotNetObjectReference.Value throws once the reference has been disposed.
        var act = () => reference.Value;
        act.Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public async Task DisposeAsync_DoesNotCallJs_WhenNeverInitialized()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        await testee.DisposeAsync();

        // Assert
        _fakeJs.Invocations.Should().NotContain(i => i.Identifier == "extensionBridge.dispose");
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

    [Test]
    public async Task OnToolResult_FailsAllPendingCalls_WhenResultJsonIsInvalid()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop");

        // Act
        testee.OnToolResult("not-valid-json{{{");
        var result = await executeTask;

        // Assert — must resolve via the parse-error broadcast, not via the (also non-empty) 1s call
        // timeout fallback; asserting the specific error text distinguishes the two completion paths.
        result.Success.Should().BeFalse();
        result.Error.Should().Be("ParseError");
    }

    [Test]
    public void OnToolResult_DoesNotMatchPendingCall_WhenResultDeserializesToNull()
    {
        // Arrange
        var testee = CreateTestee();

        // Act
        var act = () => testee.OnToolResult("null");

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public async Task OnToolResult_WhenResultDeserializesToNull_FailsPendingCallsWithEmptyResponseError()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop");

        // Act
        testee.OnToolResult("null");
        var result = await executeTask.WaitAsync(TimeSpan.FromSeconds(3));

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("EmptyExtensionResponse");
    }

    [Test]
    public void OnToolResult_IsIgnored_WhenNoPendingCallMatchesId()
    {
        // Arrange
        var testee = CreateTestee();
        var resultJson = JsonSerializer.Serialize(new ToolResult { Success = true, Id = "unknown-id" });

        // Act
        var act = () => testee.OnToolResult(resultJson);

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public async Task ExecuteToolAsync_RemovesCompletedCallFromPendingCalls_WhenToolCompletes()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        // Act
        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal), "coop");
        var callId = ExtractCallId(_fakeJs.LastSendToolCallJson);
        testee.OnToolResult(JsonSerializer.Serialize(new ToolResult { Success = true, Id = callId }));
        await executeTask;

        // Assert — the finally block must remove the completed call, otherwise pending calls would
        // leak forever and a stale entry could incorrectly match a later, unrelated call ID reuse.
        GetPendingCalls(testee).Should().BeEmpty();
    }

    [Test]
    public async Task ExecuteToolAsync_ReturnsUnsuccessfulResult_WhenToolReportsFailure()
    {
        // Arrange
        var testee = CreateTestee();
        testee.OnExtensionConnected();

        var executeTask = testee.ExecuteToolAsync("search", new Dictionary<string, object>(StringComparer.Ordinal) { ["term"] = "Tofu" }, "coop");

        var doc = JsonDocument.Parse(_fakeJs.LastSendToolCallJson);
        var callId = doc.RootElement.GetProperty("id").GetString();

        var resultJson = JsonSerializer.Serialize(new ToolResult
        {
            Success = false,
            Error = "Product not found",
            Id = callId!,
        });
        testee.OnToolResult(resultJson);

        // Act
        var result = await executeTask;

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Product not found");
    }

    [Test]
    public async Task DisposeAsync_DoesNotThrow_WhenCircuitAlreadyDisconnected()
    {
        // Arrange
        var testee = CreateTestee();
        await testee.InitializeAsync();
        _fakeJs.ThrowJsDisconnectedOnDispose = true;

        // Act
        var act = async () => await testee.DisposeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static string ExtractCallId(string sendToolCallJson) =>
        JsonDocument.Parse(sendToolCallJson).RootElement.GetProperty("id").GetString()!;

    private static ConcurrentDictionary<string, TaskCompletionSource<ToolResult>> GetPendingCalls(ExtensionBridge testee) =>
        (ConcurrentDictionary<string, TaskCompletionSource<ToolResult>>)typeof(ExtensionBridge)
            .GetField("_pendingCalls", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(testee)!;

    private ExtensionBridge CreateTestee(int toolCallTimeoutSeconds = 1) =>
        new(_fakeJs, _localizerMock, NullLogger<ExtensionBridge>.Instance, Options.Create(new ExtensionOptions { ToolCallTimeoutSeconds = toolCallTimeoutSeconds }));

    private sealed class FakeJSRuntime : IJSRuntime
    {
        public List<(string Identifier, object?[] Args)> Invocations { get; } = [];

        public string LastSendToolCallJson =>
            Invocations.LastOrDefault(i => string.Equals(i.Identifier, "extensionBridge.sendToolCall", StringComparison.Ordinal)).Args
                .ElementAtOrDefault(0) as string ?? string.Empty;

        public bool ThrowJsDisconnectedOnDispose { get; set; }

#pragma warning disable SA1011
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add((identifier, args ?? []));

            if (ThrowJsDisconnectedOnDispose && string.Equals(identifier, "extensionBridge.dispose", StringComparison.Ordinal))
            {
                throw new JSDisconnectedException("Circuit already disconnected");
            }

            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
#pragma warning restore SA1011
    }
}
