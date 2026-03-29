using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using ShoppingAgent.Diagnostics;
using ShoppingAgent.Options;
using ShoppingAgent.Services;
using ShoppingAgent.Services.Concrete;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// HTTP client for ShopAndEat API (preferences, mappings)
// Base address points to /shopAndEat/ so that relative paths like "api/..." resolve correctly.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), "/shopAndEat/") });

// Configuration – Options pattern
builder.Services.Configure<LlmClientOptions>(builder.Configuration.GetSection(LlmClientOptions.SectionName));
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection(AgentOptions.SectionName));
builder.Services.Configure<ExtensionOptions>(builder.Configuration.GetSection(ExtensionOptions.SectionName));
builder.Services.Configure<ShopOptions>(builder.Configuration.GetSection(ShopOptions.SectionName));

// Localization
builder.Services.AddLocalization();

// Metrics – registers IMeterFactory required by ShoppingAgentMetrics
builder.Services.AddMetrics();
builder.Services.AddSingleton<ShoppingAgentMetrics>();

builder.Services.AddSingleton(TimeProvider.System);

// LLM client provider (Mistral, API key in memory only) – uses its own HttpClient with no base address
builder.Services.AddScoped<IMistralChatClientProvider>(sp => new MistralChatClientProvider(
    new HttpClient(),
    sp.GetRequiredService<ILoggerFactory>().CreateLogger<MistralChatClientProvider>(),
    sp.GetRequiredService<IOptions<LlmClientOptions>>()));

// Shopping agent services
builder.Services.AddScoped<IExtensionBridge, ExtensionBridge>();
builder.Services.AddScoped<IShopToolExecutorFactory, ShopToolExecutorFactory>();
builder.Services.AddScoped<IPreferencesService, PreferencesService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ISystemPromptBuilder, SystemPromptBuilder>();
builder.Services.AddScoped<IToolDefinitionProvider, ToolDefinitionProvider>();
builder.Services.AddScoped<IToolCallDispatcher, ToolCallDispatcher>();
builder.Services.AddScoped<IToolResultRenderer, HtmlToolResultRenderer>();
builder.Services.AddScoped<IConversationManager, ConversationManager>();
builder.Services.AddScoped<IShopSessionManager, ShopSessionManager>();
builder.Services.AddScoped<IAgentService, AgentService>();

var host = builder.Build();

// Synchronize WASM culture with server-rendered <html lang="..."> attribute
var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
var browserLanguage = await jsRuntime.InvokeAsync<string>("eval", "document.documentElement.lang || navigator.language || 'en'");
var cultureName = browserLanguage.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? "de" : "en";
var culture = new CultureInfo(cultureName);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
