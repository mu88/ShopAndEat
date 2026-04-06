![Combined CI / Release](https://github.com/mu88/ShopAndEat/actions/workflows/CI_CD.yml/badge.svg)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=coverage)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=bugs)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=mu88_ShopAndEat&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=mu88_ShopAndEat)

## Modernization Backlog

The ShoppingAgent feature introduced modern .NET patterns. The following items track applying these patterns to the legacy parts of the application (existing entities, controllers, and services). Read `Directory.Build.props` for .NET version, `Directory.Packages.props` for package versions, and `.config/dotnet-tools.json` for tool versions — do not hardcode these values.

### ShoppingAgent Architecture
- [ ] Split `ConversationManager` into focused services: separate LLM communication, tool execution orchestration, and fallback/retry logic
- [ ] `IConversationManager.ProcessAsync` accepts `List<ChatMessage>` because the implementation mutates it (adds assistant/tool messages). Consider redesigning to return new messages instead of mutating the shared list, then change the parameter to `IReadOnlyList<ChatMessage>`

### Nullable Reference Types
- [ ] Enable NRTs globally across the solution (`<Nullable>enable</Nullable>` in `Directory.Build.props`) and fix all resulting warnings — currently only the ShoppingAgent project has NRTs enabled

### Entity Layer (`DataLayer/EfClasses/`)
- [ ] Add constructors + `private set` to `Article`, `Recipe` (like `ShoppingSession`, `OnlineArticleMapping`)
- [ ] Create strongly typed IDs (`ArticleId`, `MealId`, `RecipeId`, `UnitId`, `StoreId`, `ArticleGroupId`) as `readonly record struct` (pattern: see `ShoppingSessionId.cs`)
- [ ] Register ID value conversions in `EfCoreContext.OnModelCreating` via `HasConversion`
- [ ] Update all repositories and callers to use typed IDs instead of `int`

### Controller Layer (`ShopAndEat/Api/`)
- [ ] Migrate `UnitsController` to `TypedResults` return types (like `SessionsController`)
- [ ] Add `ProblemDetails` error responses where missing
- [ ] Add `CancellationToken` parameter to all controller actions (e.g., `UnitsController.GetUnits()`)
- [ ] Thread `CancellationToken` through all service and repository async calls

### Service/Business Layer (`ServiceLayer/`, `BizLogic/`, `BizDbAccess/`)
- [ ] Inject `TimeProvider` instead of `DateTime.Now`/`DateTimeOffset.UtcNow`
- [ ] Review return types: use `IEnumerable<T>` when only iterating, `IReadOnlyList<T>` when count/index needed
- [ ] Make all async service methods accept `CancellationToken`

### DTO Layer (`DTO/`)
- [ ] Ensure all DTOs use `record` with `init` properties (fix `NewMealDto` mutable properties)
- [ ] Verify all DTOs have mapping extension methods (`ToDto()`, `ToEntity()`)

### Cross-Cutting
- [ ] Add `IStringLocalizer` + `.resx` for user-facing strings in legacy controllers
- [ ] Add `ActivitySource` instrumentation to old service operations for OpenTelemetry tracing
- [ ] Evaluate `IOptions<T>` pattern for any hardcoded configuration values in services

### Observability
- ✅ **Resolved**: ShoppingAgent now runs server-side (Blazor Server / InteractiveServer). All traces and metrics are emitted directly into the server-side OpenTelemetry pipeline and reach the Aspire Dashboard.

### Testing
- ✅ **Resolved**: No WASM app to boot — the ShoppingAgent Razor components render server-side via SignalR. Standard `HttpClient`-based system tests cover the full application.