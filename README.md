# ShopAndEat

![Combined CI / Release](https://github.com/mu88/ShopAndEat/actions/workflows/CI_CD.yml/badge.svg)
![Mutation testing](https://github.com/mu88/ShopAndEat/actions/workflows/Mutation%20Testing.yml/badge.svg)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=bugs)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=coverage)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Mutation testing badge](https://img.shields.io/endpoint?style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fmu88%2FShopAndEat%2Fmain)](https://dashboard.stryker-mutator.io/reports/github.com/mu88/ShopAndEat/main)

A self-hosted meal-planning and grocery-shopping app built with ASP.NET Core Blazor Server. Plan meals from recipes, derive a consolidated shopping list of missing ingredients, and let an LLM-powered **Shopping Agent** drive an actual online grocery cart (currently [Coop.ch](https://www.coop.ch)) through a companion browser extension.

## Features

- **Meal planning**: schedule recipes onto a calendar; articles and units are managed centrally and reused across recipes
- **Shopping list generation**: derives the consolidated list of missing ingredients for a given time range, considering what's already in stock
- **Shopping Agent**: a chat-based assistant (Mistral LLM via `Microsoft.Extensions.AI`) that takes the shopping list, searches products on the configured online shop, asks for clarification on ambiguous items, and fills the real shopping cart - with resilient retry/fallback-model handling for rate limits and transient failures
- **Browser extension bridge**: a Manifest V3 extension executes the Shopping Agent's tool calls (product search, add-to-cart, navigation) directly on the shop's website via a JS-interop bridge (postMessage relay through Blazor Server), so no shop API integration is required
- **Preferences**: the agent remembers confirmed product choices and reminders per article, scoped per shop
- **OpenTelemetry tracing & metrics**: both the classic web app and the Shopping Agent emit traces/metrics to an OTLP endpoint (e.g. the .NET Aspire Dashboard)

## Architecture

| Project | Responsibility |
|---|---|
| `DataLayer` | EF Core entities, `DbContext`, migrations |
| `DTO` | API/UI data-transfer records, mapped from/to entities |
| `BizDbAccess` | Repository layer around `DataLayer` |
| `BizLogic` | Legacy business logic (meals, recipes, ingredient lists) |
| `ServiceLayer` | Modern service layer (typed IDs, `CancellationToken` throughout, `TimeProvider`) |
| `ShoppingAgent` | The LLM-driven chat assistant: conversation/tool-call orchestration, resilient `IChatClient` decorator, Razor UI |
| `ShopAndEat` | ASP.NET Core host: API controllers, classic Blazor pages (meals/recipes/articles), DI wiring |
| `BrowserExtension` | Manifest V3 extension that bridges the Shopping Agent's tool calls to the real shop website |

The `ShoppingAgent` feature slice follows modern .NET patterns throughout (strongly-typed IDs, DTOs-as-records, `TimeProvider`, primary constructors, `[LoggerMessage]`); the legacy meal/recipe/article pages use an older, more traditional layering that is being incrementally modernized.

## Local Development

### Prerequisites

- .NET SDK (see `global.json` for the exact version)
- A [Mistral AI](https://console.mistral.ai/) API key for the Shopping Agent

### Run

```bash
dotnet user-secrets set "LlmClient:ApiKey" "<your-mistral-api-key>" --project ShopAndEat
dotnet run --project ShopAndEat
```

The app listens on the port configured in `ShopAndEat/Properties/launchSettings.json`. On first start it creates a SQLite database at the path configured under `ConnectionStrings:SQLite`.

### Test

```bash
dotnet build ShopAndEat.slnx --no-restore
dotnet test ShopAndEat.slnx --no-build
```

The suite is NUnit + FluentAssertions + NSubstitute + bUnit (for Razor components), verified with [Stryker](https://stryker-mutator.io/) mutation testing. A few `Category=System` tests spin up the app via `WebApplicationFactory`/Testcontainers and are slower - filter them out for quick local iteration:

```bash
dotnet test ShopAndEat.slnx --no-build --filter "Category!=System"
```

Live, opt-in integration tests against a real Mistral endpoint live under `Tests/LlmIntegration/` - see [below](#live-llm-integration-tests).

### Browser Extension

Load `BrowserExtension/` as an unpacked extension (`chrome://extensions` → Developer mode → Load unpacked). It connects back to the running `ShopAndEat` instance via SignalR to execute the Shopping Agent's tool calls on the shop's website.

## Configuration

Key sections in `appsettings.json` (see the file for the full set of defaults):

- `ConnectionStrings:SQLite` - path to the SQLite database file
- `LlmClient` - Mistral endpoint, default/fallback model, timeout and retry settings
- `Agent` - tool-calling loop limits, retry and model-fallback toggles
- `Extension` - timeout for the browser-extension tool-call bridge
- `Shops` - configured online shops (key, display name, base/cart URLs)

### Docker

```bash
docker compose up
```

`docker-compose.yml` wires up the app together with an [Aspire Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview) for viewing traces/metrics, and reads the Mistral API key from a Docker secret at `./secrets/LlmClient__ApiKey`.

## Live LLM Integration Tests

The `Tests/LlmIntegration/` test suite validates the ShoppingAgent end-to-end with a real Mistral API client. These tests are **opt-in only** and excluded from normal CI/local test runs.

### Prerequisites

- A valid Mistral API key: https://console.mistral.ai/
- The key must be set via the `LlmClient__ApiKey` environment variable

### Running Live Tests Locally

```powershell
$env:LlmClient__ApiKey = 'your-mistral-api-key'
dotnet test --filter "Category=LlmIntegration"
```

Or run a single scenario:

```bash
dotnet test --filter "Category=LlmIntegration&Name~ResearchPhase_SearchProducts"
```

### Test Coverage

The suite covers all user-facing ShoppingAgent flows: research (product search, details, preferences), clarification, confirmation, cart operations, shop switching, multi-step end-to-end scenarios, and resilience (empty results, consecutive requests, state preservation across turns).

- Tests use a **real Mistral client** to exercise actual LLM reasoning
- A **scripted shop tool executor** provides deterministic, repeatable behavior (no real shop API calls)
- **Behavioral assertions only**: tests verify tool execution and state transitions, not exact LLM wording
- Tests are marked `[Explicit]` and tagged `[Category("LlmIntegration")]`, so they never run unless explicitly requested
- Running the full suite (~19 tests) typically costs less than $0.01 USD in Mistral API usage

## License

[Do No Harm License](LICENSE.md)
