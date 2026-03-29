using System.Text.Json.Serialization;
using BizDbAccess;
using BizDbAccess.Concrete;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using Microsoft.EntityFrameworkCore;
using mu88.Shared.OpenTelemetry;
using OpenTelemetry;
using Scalar.AspNetCore;
using ServiceLayer;
using ServiceLayer.Concrete;
using ShopAndEat.Components;
using ShoppingAgent.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureOpenTelemetry("shopandeat", builder.Configuration);

// Extend OTel with custom ShoppingAgent ActivitySource and Meter
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(ShoppingAgentDiagnostics.ActivitySourceName))
    .WithMetrics(metrics => metrics.AddMeter(ShoppingAgentDiagnostics.MeterName));

ConfigureShopAndEatServices(builder.Services, builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
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
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ShoppingAgent.Pages.Home).Assembly);

await app.RunAsync();

void CreateDbIfNotExists(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var database = services.GetRequiredService<EfCoreContext>().Database;
        var connectionString = database.GetConnectionString();
        var databasePath = connectionString?.Replace("Data Source=", string.Empty);
        var parentDirectoryOfDatabase = Directory.GetParent(databasePath);
        if (!parentDirectoryOfDatabase.Exists)
        {
            Directory.CreateDirectory(parentDirectoryOfDatabase.FullName);
        }

        database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred migrating the DB");
    }
}

void ConfigureShopAndEatServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton(TimeProvider.System);
    services.AddSingleton<ShoppingAgentMetrics>();
    services.AddScoped<ISessionRepository, SessionRepository>();
    services.AddScoped<IPreferencesRepository, PreferencesRepository>();
    services.AddScoped<IArticleMappingRepository, ArticleMappingRepository>();
    services.AddTransient<SimpleCrudHelper>();
    services.AddTransient<IMealService, MealService>();
    services.AddTransient<IStoreService, StoreService>();
    services.AddTransient<IRecipeService, RecipeService>();
    services.AddTransient<IMealTypeService, MealTypeService>();
    services.AddTransient<IUnitService, UnitService>();
    services.AddTransient<IArticleService, ArticleService>();
    services.AddTransient<IArticleGroupService, ArticleGroupService>();
    services.AddTransient<IArticleAction, ArticleAction>();
    services.AddTransient<IArticleDbAccess, ArticleDbAccess>();
    services.AddTransient<IGeneratePurchaseItemsForRecipesAction, GeneratePurchaseItemsForRecipesAction>();
    services.AddTransient<IOrderPurchaseItemsByStoreAction, OrderPurchaseItemsByStoreAction>();
    services.AddTransient<IGetRecipesForMealsAction, GetRecipesForMealsAction>();

    services.AddDbContext<EfCoreContext>(options => options.UseLazyLoadingProxies().UseSqlite(configuration.GetConnectionString("SQLite")));

    services.AddHealthChecks().AddDbContextCheck<EfCoreContext>();
}
