using System.Diagnostics.Metrics;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
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
    private static BunitContext CreateBunitContext(IChatClient mockChatClient = null)
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
        ctx.Services.AddSingleton<ISystemPromptBuilder, SystemPromptBuilder>();
        ctx.Services.AddSingleton<IToolDefinitionProvider, ToolDefinitionProvider>();
        ctx.Services.AddSingleton<IToolCallDispatcher, ToolCallDispatcher>();
        ctx.Services.AddSingleton<IToolResultRenderer, HtmlToolResultRenderer>();
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
        cut.Find("textarea").Input("Hello World");
        cut.Find(".send-button").Click();

        // Assert
        cut.WaitForAssertion(() =>
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
        cut.Find("textarea").Input("Find milk");
        cut.Find(".send-button").Click();

        // Assert
        cut.WaitForAssertion(() =>
            cut.Find(".chat-message.assistant .message-text").TextContent.Should().Contain("I can help you shop!"));
    }

    [Test]
    public async Task ClearChat_ClearsMessages()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.Messages.Add(new ChatMessage { Role = "user", Content = "Test message" });
        cut.Render(_ => { });

        cut.FindAll(".chat-message").Should().HaveCount(1);

        // Act
        await cut.Find("[data-testid='clear-chat']").ClickAsync();

        // Assert
        cut.WaitForAssertion(() =>
            cut.FindAll(".chat-message").Should().BeEmpty());
    }

    [Test]
    public async Task MarkdownRendering_AllowsSafeHtml()
    {
        // Arrange
        await using var ctx = CreateBunitContext();
        var cut = ctx.Render<global::ShoppingAgent.Pages.Home>();

        var agent = ctx.Services.GetRequiredService<IAgentService>();
        agent.Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = "<details class=\"tool-group\"><summary>Test</summary>Result</details>",
        });
        cut.Render(_ => { });

        // Act
        var messageText = cut.Find(".chat-message.assistant .message-text");

        // Assert - safe HTML like <details> should be rendered as HTML, not escaped
        messageText.InnerHtml.Should().Contain("<details");
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
        textarea.Input("Hello World");
        await textarea.KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = false });

        // Assert
        cut.WaitForAssertion(() =>
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
        textarea.Input("Hello World");
        await textarea.KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        // Assert
        cut.FindAll(".chat-message").Should().BeEmpty();
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
        cut.WaitForAssertion(() =>
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
        cut.WaitForAssertion(() =>
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
        cut.Find("select").Change("migros");

        // Assert
        cut.WaitForAssertion(() =>
            agentMock.Received(1).SwitchShopAsync("migros", Arg.Any<CancellationToken>()));
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
}
