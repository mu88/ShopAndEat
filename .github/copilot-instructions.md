# ShopAndEat Repository Instructions

## Project Structure
Layered .NET application with a Blazor Server frontend and a Blazor WebAssembly sub-project for the AI shopping agent.

- `DataLayer/` — EF Core entities (`EfClasses/`), `EfCoreContext`, migrations
- `BizDbAccess/` — Repository classes for database access
- `BizLogic/` — Business logic, independent of web framework
- `ServiceLayer/` — Service interfaces + implementations consumed by controllers
- `DTO/` — DTOs and mapping extension methods (`ToDto()`, `ToEntity()`)
- `ShopAndEat/` — ASP.NET Core Blazor Server host. API controllers in `Api/`, Razor components, `Program.cs`
- `ShoppingAgent/` — Blazor WebAssembly (`Microsoft.NET.Sdk.BlazorWebAssembly`). **MUST remain a separate project** — WASM compilation requires this SDK. Hosted via `AddAdditionalAssemblies()`.
- `ShoppingClient/` — HTTP client for the ShoppingAgent WASM app
- `BrowserExtension/` — Browser extension (excluded from Sonar analysis)
- `Tests/` — Single test project with subdirectories `Unit/`, `Integration/`, `System/`

## Repo-Specific Configuration Files
In addition to the standard files (read dynamically, never hardcode values):
- `coverage.settings.xml` — module paths included in coverage collection
- `SonarQube.Analysis.xml` — Sonar coverage and general exclusions

## Modernization Backlog
See `README.md` for legacy patterns to modernize. When working on legacy code, apply the modern patterns listed there.
