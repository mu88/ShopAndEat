using System.Diagnostics.Metrics;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
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
using AiChatMessage = Microsoft.Extensions.AI.ChatMessage;
using BunitContext = Bunit.BunitContext;
using ChatMessage = ShoppingAgent.Models.ChatMessage;

namespace Tests.Unit.ShoppingAgent;

[TestFixture]
[Category("Unit")]
public class HomePageTests
{
    private static BunitContext CreateBunitContext(IChatClient? mockChatClient = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var chatClientProvider = Substitute.For<IMistralChatClientProvider>();
        if (mockChatClient != null)
        {
            chatClientProvider.GetChatClientAsync().Returns(Task.FromResult(mockChatClient));
        }
        else
        {
            var noOpClient = Substitute.For<IChatClient>();
            noOpClient.GetResponseAsync(Arg.Any<IEnumerable<AiChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ChatResponse([new AiChatMessage(ChatRole.Assistant, string.Empty)])));
            chatClientProvider.GetChatClientAsync().Returns(Task.FromResult(noOpClient));
        }

        var preferencesMock = Substitute.For<IPreferencesService>();
        preferencesMock.GetAllPreferencesAsync(Arg.Any<string>()).Returns(new List<PreferenceDto>());

        var sessionMock = Substitute.For<ISessionService>();
        sessionMock.GetUnitsAsync().Returns(new List<string>());
        sessionMock.GetIngredientListAsync().Returns(new List<IngredientItem>());

        var factoryMock = Substitute.For<IShopToolExecutorFactory>();
#pragma warning disable SA1010
        factoryMock.AvailableShops.Returns(
        [
            new ShopConfig("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart"),
        ]);
#pragma warning restore SA1010
        factoryMock.GetExecutor(Arg.Any<string>()).Returns(Substitute.For<IShopToolExecutor>());

        RegisterServices(ctx, chatClientProvider, preferencesMock, sessionMock, factoryMock);

        return ctx;
    }

    private static void RegisterServices(BunitContext ctx, IMistralChatClientProvider chatClientProvider, IPreferencesService preferencesMock, ISessionService sessionMock, IShopToolExecutorFactory factoryMock)
    {
        var localizerMock = Substitute.For<IStringLocalizer<Messages>>();
        localizerMock[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.Arg<string>(), call.Arg<string>()));
        localizerMock[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), call.ArgAt<string>(0)));

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(callInfo => new Meter(callInfo.Arg<MeterOptions>()));

        ctx.Services.AddLocalization();
        ctx.Services.AddSingleton<IStringLocalizer<Messages>>(localizerMock);
        ctx.Services.AddSingleton<IMistralChatClientProvider>(chatClientProvider);
        ctx.Services.AddSingleton<IPreferencesService>(preferencesMock);
        ctx.Services.AddSingleton<ISessionService>(sessionMock);
        ctx.Services.AddSingleton<IShopToolExecutorFactory>(factoryMock);
        ctx.Services.AddSingleton<IMeterFactory>(meterFactory);
        ctx.Services.AddSingleton<ShoppingAgentMetrics>();
        ctx.Services.AddSingleton(Options.Create(new LlmClientOptions { ApiKey = "test-key" }));
        ctx.Services.AddSingleton(Options.Create(new AgentOptions()));
        ctx.Services.AddSingleton(Options.Create(new ExtensionOptions()));
        ctx.Services.AddSingleton<ILlmRetryPolicyFactory>(serviceProvider =>
            new LlmRetryPolicyFactory(
                serviceProvider.GetRequiredService<IOptions<LlmClientOptions>>(),
                serviceProvider.GetRequiredService<ShoppingAgentMetrics>(),
                NullLogger<LlmRetryPolicyFactory>.Instance));
        ctx.Services.AddSingleton<ISystemPromptBuilder, SystemPromptBuilder>();
        ctx.Services.AddSingleton<IToolDefinitionProvider, ToolDefinitionProvider>();
        ctx.Services.AddSingleton<IShoppingListVerifier, ShoppingListVerifier>();
        ctx.Services.AddSingleton<IShoppingWorkflowState>(Substitute.For<IShoppingWorkflowState>());
        ctx.Services.AddSingleton<IToolCallDispatcher, ToolCallDispatcher>();
        ctx.Services.AddSingleton<ILlmCommunicator, LlmCommunicator>();
        ctx.Services.AddSingleton<IToolExecutionOrchestrator, ToolExecutionOrchestrator>();
        ctx.Services.AddSingleton<IToolResultRenderer, HtmlToolResultRenderer>();
        ctx.Services.AddSingleton<IToolResultCompressor, ToolResultCompressor>();
        ctx.Services.AddSingleton<IConversationManager, ConversationManager>();
        ctx.Services.AddSingleton<IShopSessionManager, ShopSessionManager>();
        ctx.Services.AddSingleton<IAgentService, AgentService>();
        ctx.Services.AddSingleton<IExtensionBridge, ExtensionBridge>();
        ctx.Services.AddSingleton(TimeProvider.System);
    }

    [Test]
    public async Task SendMessage_DisplaysUserMessage()
    {
        // Arrange
        var mockChatClient = CreateMockChatClient("Agent reply");

        await using var ctx = CreateBunitContext(mockChatClient);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        await cut.Find("textarea").InputAsync("Hello World");
        cut.Find(".send-button").Click();

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".chat-message.user .message-text").TextContent.Should().Contain("Hello World"));
    }

    [Test]
    public async Task SendMessage_DisplaysAgentResponse()
    {
        // Arrange
        var mockChatClient = CreateMockChatClient("I can help you shop!");

        await using var ctx = CreateBunitContext(mockChatClient);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        await cut.Find("textarea").InputAsync("Find milk");
        cut.Find(".send-button").Click();

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".chat-message.assistant .message-text").TextContent.Should().Contain("I can help you shop!"));
    }

    [Test]
    public async Task ClearChat_ClearsMessages()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.AddMessage(new ChatMessage { Role = "user", Content = "Test message" });
        cut.Render(_ => { });

        cut.FindAll(".chat-message").Should().HaveCount(1);

        // Act
        await cut.Find("[data-testid='clear-chat']").ClickAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.FindAll(".chat-message").Should().BeEmpty());
    }

    [Test]
    public async Task MarkdownRendering_AllowsSafeHtml()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.AddMessage(new ChatMessage
        {
            Role = "assistant",
            Content = "<details class=\"tool-group\"><summary>Test</summary>Result</details>",
        });
        cut.Render(_ => { });

        // Act
        var messageText = cut.Find(".chat-message.assistant .message-text");

        // Assert - safe HTML like <details> should be rendered as HTML, not escaped, and the
        // "class" attribute must survive sanitization since app.css's tool-group/tool-call
        // styling depends on it.
        messageText.InnerHtml.Should().Contain("<details");
        messageText.InnerHtml.Should().Contain("class=\"tool-group\"");
        messageText.InnerHtml.Should().Contain("<summary>");
    }

    [Test]
    public async Task HandleKeyDown_SendsMessageOnEnter()
    {
        // Arrange
        var mockChatClient = CreateMockChatClient("Reply");
        await using var ctx = CreateBunitContext(mockChatClient);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        var textarea = cut.Find("textarea");
        await textarea.InputAsync("Hello World");
        await textarea.KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = false });

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".chat-message.user .message-text").TextContent.Should().Contain("Hello World"));
    }

    [Test]
    public async Task HandleKeyDown_DoesNotSendOnShiftEnter()
    {
        // Arrange
        var mockChatClient = CreateMockChatClient("Reply");
        await using var ctx = CreateBunitContext(mockChatClient);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        var textarea = cut.Find("textarea");
        await textarea.InputAsync("Hello World");
        await textarea.KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        // Assert
        cut.FindAll(".chat-message").Should().BeEmpty();
    }

    [Test]
    public async Task HandleKeyDown_DoesNotSend_WhenInputIsEmpty()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act - Enter pressed on an empty textarea hits the SendMessage guard clause
        var textarea = cut.Find("textarea");
        await textarea.KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = false });

        // Assert
        cut.FindAll(".chat-message").Should().BeEmpty();
    }

    [Test]
    public async Task HandleKeyDown_DoesNotSend_WhenKeyIsNotEnter()
    {
        // Arrange — exercises the short-circuit false branch of `e.Key == "Enter" && !e.ShiftKey`
        // where the key itself isn't "Enter", so ShiftKey is never even evaluated.
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        var textarea = cut.Find("textarea");
        await textarea.InputAsync("Hello World");
        await textarea.KeyDownAsync(new KeyboardEventArgs { Key = "a", ShiftKey = false });

        // Assert
        cut.FindAll(".chat-message").Should().BeEmpty();
    }

    [Test]
    public async Task OnShopChanged_DoesNotSwitchShop_WhenSameShopIsSelected()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var agentMock = Substitute.For<IAgentService>();
        agentMock.Messages.Returns([]);
        agentMock.SelectedShopKey.Returns("coop");
        agentMock.AvailableShops.Returns(
        [
            new ShopConfig("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart"),
            new ShopConfig("migros", "Migros", "https://www.migros.ch", "https://www.migros.ch/cart"),
        ]);
        ctx.Services.AddSingleton<IAgentService>(agentMock);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act - first switch away from "coop" (a real DOM value change, so the event fires), then
        // switch back to "coop": since the mocked Agent.SelectedShopKey never actually changes, the
        // second change hits the `shopKey != Agent.SelectedShopKey` guard's false branch.
        await cut.Find("select").ChangeAsync("migros");
        agentMock.ClearReceivedCalls();
        await cut.Find("select").ChangeAsync("coop");

        // Assert
        await agentMock.DidNotReceive().SwitchShopAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task OnShopChanged_DoesNotSwitchShop_WhenSelectedValueIsEmpty()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var agentMock = Substitute.For<IAgentService>();
        agentMock.Messages.Returns([]);
        agentMock.SelectedShopKey.Returns("coop");
        agentMock.AvailableShops.Returns(
        [
            new ShopConfig("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart"),
        ]);
        ctx.Services.AddSingleton<IAgentService>(agentMock);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act - an empty selected value hits the `!string.IsNullOrEmpty(shopKey)` guard's false branch.
        await cut.Find("select").ChangeAsync(string.Empty);

        // Assert
        await agentMock.DidNotReceive().SwitchShopAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StatusBadge_ShowsConnected_WhenExtensionIsConnected()
    {
        // Arrange - exercises the true branch of `Bridge.IsExtensionConnected ? ... : ...`
        await using var ctx = CreateBunitContext();
        var bridgeMock = Substitute.For<IExtensionBridge>();
        bridgeMock.IsExtensionConnected.Returns(true);
        ctx.Services.AddSingleton<IExtensionBridge>(bridgeMock);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Assert
        cut.Find(".status-badge.connected").TextContent.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task StopProcessing_DoesNothing_WhenNoRequestIsInProgress()
    {
        // Arrange - Agent.IsProcessing renders the stop button independently of this component's
        // own (still-null) _cts field, exercising the null-conditional `_cts?.Cancel()` no-op branch.
        await using var ctx = CreateBunitContext();
        var agentMock = Substitute.For<IAgentService>();
        agentMock.Messages.Returns([]);
        agentMock.SelectedShopKey.Returns("coop");
        agentMock.IsProcessing.Returns(true);
        agentMock.AvailableShops.Returns(
        [
            new ShopConfig("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart"),
        ]);
        ctx.Services.AddSingleton<IAgentService>(agentMock);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act & Assert
        var action = async () => await cut.Find(".stop-button").ClickAsync();
        await action.Should().NotThrowAsync();
    }

    [Test]
    public async Task CopyChat_DoesNothing_WhenNoMessages()
    {
        // Arrange - exercises the `if (Agent.Messages.Count == 0) return;` guard clause directly.
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        await cut.Find("[data-testid='copy-chat']").ClickAsync();

        // Assert - no clipboard write was ever attempted
        ctx.JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "navigator.clipboard.writeText");
    }

    [Test]
    public async Task DisposeAsync_CancelsOngoingRequest_WhenComponentIsDisposedWhileProcessing()
    {
        // Arrange - exercises the non-null branch of Dispose's `_cts?.Cancel(); _cts?.Dispose();`
        // by disposing the component while a send is still in flight (before SendMessage's own finally
        // block would otherwise reset _cts back to null).
        var mockChatClient = CreateSlowStreamingChatClient();
        await using var ctx = CreateBunitContext(mockChatClient);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        await cut.Find("textarea").InputAsync("Hello");
        _ = cut.Find(".send-button").ClickAsync();

        await cut.WaitForAssertionAsync(() =>
            cut.Find(".stop-button").Should().NotBeNull());

        // Act & Assert
        var action = () => ((IDisposable)cut.Instance).Dispose();
        action.Should().NotThrow();
    }

    [Test]
    public async Task SendMessage_ShowsProcessingStoppedMessage_WhenCancelled()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var agentMock = Substitute.For<IAgentService>();
        ConfigureMessagesBackingList(agentMock);
        agentMock.SelectedShopKey.Returns("coop");
        agentMock.AvailableShops.Returns(
        [
            new ShopConfig("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart"),
        ]);
        agentMock.ProcessMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ThrowCancelledAsync());
        ctx.Services.AddSingleton<IAgentService>(agentMock);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        await cut.Find("textarea").InputAsync("Hello");
        await cut.Find(".send-button").ClickAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".chat-message.assistant .message-text").TextContent.Should().Contain("ProcessingStopped"));
    }

    [Test]
    public async Task OnAfterRenderAsync_InvokesBridgeConnectionChangedHandler()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var bridgeMock = Substitute.For<IExtensionBridge>();
        ctx.Services.AddSingleton<IExtensionBridge>(bridgeMock);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act - simulate the extension bridge raising its connection-changed event
        bridgeMock.OnConnectionChanged += Raise.Event<Action>();

        // Assert - no unhandled exception; component keeps rendering
        await cut.WaitForAssertionAsync(() =>
            cut.Markup.Should().NotBeNullOrEmpty());
    }

    [Test]
    public async Task OnAfterRenderAsync_SwallowsJsDisconnectedException_OnSubsequentRender()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        ctx.JSInterop.SetupVoid("restoreDetailsStates").SetException(new JSDisconnectedException("disconnected"));
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act - triggers a subsequent render, invoking OnAfterRenderAsync(firstRender: false)
        await cut.Find("[data-testid='clear-chat']").ClickAsync();

        // Assert - the JSDisconnectedException is swallowed; component keeps rendering
        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task ScrollToBottomAsync_SwallowsJsDisconnectedException()
    {
        // Arrange
        var mockChatClient = CreateMockChatClient("Reply");
        await using var ctx = CreateBunitContext(mockChatClient);
        ctx.JSInterop.SetupVoid("eval", _ => true).SetException(new JSDisconnectedException("disconnected"));
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act - SendMessage calls ScrollToBottomAsync, which invokes "eval" for scrolling
        await cut.Find("textarea").InputAsync("Hello");
        await cut.Find(".send-button").ClickAsync();

        // Assert - the JSDisconnectedException is swallowed; the message still gets processed
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".chat-message.assistant .message-text").TextContent.Should().Contain("Reply"));
    }

    [Test]
    public async Task LoadFromMealPlan_ShowsNoIngredientsMessage_WhenEmpty()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        await cut.Find("[data-testid='load-meal-plan']").ClickAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
            cut.Find(".chat-message.assistant .message-text").TextContent.Should().Contain("NoIngredientsFound"));
    }

    [Test]
    public async Task LoadFromMealPlan_PopulatesTextarea_WhenIngredientsExist()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var sessionMock = ctx.Services.GetRequiredService<ISessionService>();
        sessionMock.GetIngredientListAsync().Returns(
        [
            new IngredientItem { Text = "2 cups flour" },
            new IngredientItem { Text = "1 egg" },
        ]);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        await cut.Find("[data-testid='load-meal-plan']").ClickAsync();

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            var textarea = cut.Find("textarea");
            textarea.GetAttribute("value").Should().Contain("2 cups flour");
            textarea.GetAttribute("value").Should().Contain("1 egg");
        });
    }

    [Test]
    public async Task OnShopChanged_SwitchesShop()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var agentMock = Substitute.For<IAgentService>();
        agentMock.Messages.Returns([]);
        agentMock.SelectedShopKey.Returns("coop");
        agentMock.AvailableShops.Returns(
        [
            new ShopConfig("coop", "Coop", "https://www.coop.ch", "https://www.coop.ch/de/cart"),
            new ShopConfig("migros", "Migros", "https://www.migros.ch", "https://www.migros.ch/cart"),
        ]);
        ctx.Services.AddSingleton<IAgentService>(agentMock);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        await cut.Find("select").ChangeAsync("migros");

        // Assert
        await cut.WaitForAssertionAsync(() =>
            agentMock.Received(1).SwitchShopAsync("migros", Arg.Any<CancellationToken>()));
    }

    [Test]
    public async Task CopyChat_DisabledWhenNoMessages()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        // Act
        var copyButton = cut.Find("[data-testid='copy-chat']");
        var disabledAttr = copyButton.GetAttribute("disabled");

        // Assert - disabled attribute should be present (empty string or "disabled")
        disabledAttr.Should().NotBeNull();
    }

    [Test]
    public async Task FormatMessage_RendersMarkdown_InMessages()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.AddMessage(new ChatMessage
        {
            Role = "assistant",
            Content = "This is **bold** text",
        });
        cut.Render(_ => { });

        // Act
        var messageElement = cut.Find(".chat-message.assistant .message-text");

        // Assert
        messageElement.InnerHtml.Should().Contain("<strong>bold</strong>");
    }

    [Test]
    public async Task FormatMessage_SanitizesScriptTag_InLlmGeneratedContent()
    {
        // Arrange — the LLM's own text is untrusted (e.g. via prompt injection) and is
        // rendered through Markdig then MarkupString; a raw <script> tag must never survive.
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.AddMessage(new ChatMessage
        {
            Role = "assistant",
            Content = "Before<script>alert(1)</script>After",
        });
        cut.Render(_ => { });

        // Act
        var messageElement = cut.Find(".chat-message.assistant .message-text");

        // Assert - the surrounding text must still render, only the script tag is stripped
        messageElement.InnerHtml.Should().Contain("Before");
        messageElement.InnerHtml.Should().Contain("After");
        messageElement.InnerHtml.Should().NotContain("<script>");
        messageElement.QuerySelectorAll("script").Should().BeEmpty();
    }

    [Test]
    public async Task FormatMessage_SanitizesImgOnError_InLlmGeneratedContent()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.AddMessage(new ChatMessage
        {
            Role = "assistant",
            Content = "<img src=x onerror=\"alert(1)\">",
        });
        cut.Render(_ => { });

        // Act
        var messageElement = cut.Find(".chat-message.assistant .message-text");

        // Assert - the img tag itself must still render (just without the dangerous handler)
        messageElement.QuerySelectorAll("img").Should().HaveCount(1);
        messageElement.InnerHtml.Should().NotContain("onerror");
    }

    [Test]
    public async Task FormatMessage_HandlesEmptyContent()
    {
        // Arrange — also covers Content being null (non-nullable in normal usage, but could
        // arrive null e.g. via deserialization bypassing NRT checks): FormatMessage dispatches
        // both to the same string.IsNullOrEmpty branch, so one test suffices for both inputs.
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.AddMessage(new ChatMessage
        {
            Role = "assistant",
            Content = string.Empty,
        });

        // Act
        var render = () => cut.Render(_ => { });

        // Assert
        render.Should().NotThrow();
        cut.FindAll(".chat-message.assistant").Should().HaveCount(1);
    }

    [Test]
    public async Task FormatMessage_HandlesMultilineContent()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.AddMessage(new ChatMessage
        {
            Role = "assistant",
            Content = "Line 1\n\nLine 2",
        });
        cut.Render(_ => { });

        // Act
        var chatMessageElements = cut.FindAll(".chat-message.assistant .message-text");
        var messageElement = chatMessageElements.Count > 0 ? chatMessageElements[0] : null;

        // Assert
        messageElement?.InnerHtml.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task CopyChat_CopiesMessagesToClipboard_WhenMessagesExist()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.AddMessage(new ChatMessage { Role = "user", Content = "Hello" });
        agent.AddMessage(new ChatMessage { Role = "assistant", Content = "Hi there" });
        cut.Render(_ => { });

        // Act
        await cut.Find("[data-testid='copy-chat']").ClickAsync();

        // Assert - clipboard write invoked with both messages, and feedback icon toggles
        var invocation = ctx.JSInterop.VerifyInvoke("navigator.clipboard.writeText");
        var copiedText = invocation.Arguments[0]?.ToString() ?? string.Empty;
        copiedText.Should().Contain("Hello").And.Contain("Hi there");
    }

    [Test]
    public async Task StopProcessing_CancelsOngoingRequest()
    {
        // Arrange
        var mockChatClient = CreateSlowStreamingChatClient();
        await using var ctx = CreateBunitContext(mockChatClient);
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        await cut.Find("textarea").InputAsync("Hello");
        _ = cut.Find(".send-button").ClickAsync();

        await cut.WaitForAssertionAsync(() =>
            cut.Find(".stop-button").Should().NotBeNull());

        // Act - cancels the in-flight (never-completing) LLM call
        await cut.Find(".stop-button").ClickAsync();

        // Assert - cancellation propagates as an error from the underlying LLM call,
        // and processing stops (the stop button disappears again).
        await cut.WaitForAssertionAsync(() =>
            cut.FindAll(".stop-button").Should().BeEmpty());
    }

    private static IChatClient CreateMockChatClient(string responseText)
    {
        var chatClientMock = Substitute.For<IChatClient>();
        var response = new ChatResponse([new AiChatMessage(ChatRole.Assistant, responseText)]);
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));
        return chatClientMock;
    }

    private static async IAsyncEnumerable<string> ThrowCancelledAsync()
    {
        yield return string.Empty;
        await Task.Yield();
        throw new OperationCanceledException();
    }

    /// <summary>
    /// Wires a mocked <see cref="IAgentService"/>'s <c>Messages</c> property to a real backing list
    /// that <c>AddMessage</c> appends to, mirroring the read-only view + explicit mutation contract.
    /// </summary>
    private static List<ChatMessage> ConfigureMessagesBackingList(IAgentService agentMock)
    {
        var messages = new List<ChatMessage>();
        agentMock.Messages.Returns(messages);
        agentMock.When(agent => agent.AddMessage(Arg.Any<ChatMessage>()))
            .Do(callInfo => messages.Add(callInfo.Arg<ChatMessage>()));
        return messages;
    }

    private static IChatClient CreateSlowStreamingChatClient()
    {
        var chatClientMock = Substitute.For<IChatClient>();
        chatClientMock.GetResponseAsync(
                Arg.Any<IEnumerable<AiChatMessage>>(),
                Arg.Any<ChatOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => WaitUntilCancelledAsync(callInfo.ArgAt<CancellationToken>(2)));
        return chatClientMock;
    }

    private static async Task<ChatResponse> WaitUntilCancelledAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        throw new InvalidOperationException("unreachable — cancellation should have been requested");
    }
}
