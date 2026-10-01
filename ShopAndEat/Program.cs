using System.Text.Json.Serialization;
using BizDbAccess;
using BizDbAccess.Concrete;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using mu88.Shared.OpenTelemetry;
using OpenTelemetry;
using Scalar.AspNetCore;
using ServiceLayer;
using ServiceLayer.Concrete;
using ServiceLayer.Diagnostics;
using ShopAndEat;
using ShopAndEat.Components;
using ShopAndEat.Features.ShoppingAgent;
using ShoppingAgent;

var builder = WebApplication.CreateBuilder(args);

// Load Docker secrets — explicitly map known secret files to config keys.
DockerSecretsLoader.ApplyLlmApiKeySecret(builder.Configuration, "/run/secrets/llm_api_key");

// Persist Data Protection keys to tmpfs so they survive within a container session but are never written to disk.
// Keys are lost on container restart — which is acceptable since a restart disconnects all Blazor circuits anyway.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/home/app/dataprotection-keys"));

builder.Services.ConfigureOpenTelemetry("shopandeat", builder.Configuration);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(ServiceLayerDiagnostics.ActivitySourceName));

ConfigureShopAndEatServices(builder.Services, builder.Configuration);

builder.Services.EnableShoppingAgent(builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddLocalization();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

CreateDbIfNotExists(app);

app.UsePathBase("/shopAndEat");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

// Serve static files (including ShoppingAgent WASM app)
app.UseStaticFiles();

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures("en", "de")
    .AddSupportedUICultures("en", "de"));

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .MapShoppingAgent();

await app.RunAsync();

void CreateDbIfNotExists(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var database = services.GetRequiredService<EfCoreContext>().Database;
        var databasePath = DatabaseInitializer.GetDatabasePath(database.GetConnectionString());
        if (databasePath is null)
        {
            return;
        }

        DatabaseInitializer.EnsureDatabaseDirectoryExists(databasePath);

        database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "An error occurred migrating the DB; failing startup fast because this app cannot function correctly against an incompatible schema");
        throw;
    }
}

void ConfigureShopAndEatServices(IServiceCollection services, IConfiguration configuration)
{
    services.TryAddSingleton(TimeProvider.System);
    AddDataRepositories(services);
    AddDomainServices(services);
    AddDatabaseContext(services, configuration);
}

void AddDataRepositories(IServiceCollection services)
{
    services.AddScoped<ISessionRepository, SessionRepository>();
    services.AddScoped<IPreferencesRepository, PreferencesRepository>();
    services.AddScoped<IArticleMappingRepository, ArticleMappingRepository>();
    services.AddTransient<IArticleDbAccess, ArticleDbAccess>();
}

void AddDomainServices(IServiceCollection services)
{
    services.AddTransient<SimpleCrudHelper>();
    services.AddTransient<IMealService, MealService>();
    services.AddTransient<IStoreService, StoreService>();
    services.AddTransient<IRecipeService, RecipeService>();
    services.AddTransient<IMealTypeService, MealTypeService>();
    services.AddTransient<IUnitService, UnitService>();
    services.AddTransient<IArticleService, ArticleService>();
    services.AddTransient<IArticleGroupService, ArticleGroupService>();
    services.AddTransient<IArticleAction, ArticleAction>();
    services.AddTransient<IGeneratePurchaseItemsForRecipesAction, GeneratePurchaseItemsForRecipesAction>();
    services.AddTransient<IOrderPurchaseItemsByStoreAction, OrderPurchaseItemsByStoreAction>();
    services.AddTransient<IGetRecipesForMealsAction, GetRecipesForMealsAction>();
}

void AddDatabaseContext(IServiceCollection services, IConfiguration configuration)
{
    // ConfigureWarnings: dotnet-ef 10.0.10 produces a non-convergent Sqlite:Autoincrement annotation
    // diff for the value-converted (strongly-typed) primary keys on every migration scaffold, causing
    // a false-positive PendingModelChangesWarning even though the checked-in model snapshot is up to
    // date (verified by re-scaffolding twice in a row: identical diff both times, unrelated to any
    // actual entity change). Suppressed here instead of chasing further non-convergent migrations.
    // NonTransactionalMigrationOperationWarning is expected and benign here: SQLite has no native
    // ALTER COLUMN, so EF Core emulates it via a full table rebuild wrapped in
    // "PRAGMA foreign_keys = 0/1", which cannot run inside a transaction by design.
    services.AddDbContext<EfCoreContext>(options => options.UseLazyLoadingProxies()
                                                            .UseSqlite(configuration.GetConnectionString("SQLite"))
                                                            .ConfigureWarnings(warnings => warnings
                                                                .Ignore(RelationalEventId.PendingModelChangesWarning)
                                                                .Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning)));

    services.AddHealthChecks().AddDbContextCheck<EfCoreContext>();
}
