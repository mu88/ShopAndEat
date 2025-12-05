using System;
using System.IO;
using BizDbAccess;
using BizDbAccess.Concrete;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mu88.Shared.OpenTelemetry;
using Scalar.AspNetCore;
using ServiceLayer;
using ServiceLayer.Concrete;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureOpenTelemetry("ShopAndEat");

ConfigureShopAndEatServices(builder.Services, builder.Configuration);

builder.Services.AddHealthChecks();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

CreateDbIfNotExists(app);

app.UsePathBase("/shopAndEat");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAuthorization();

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();

void CreateDbIfNotExists(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        DatabaseFacade database = services.GetRequiredService<EfCoreContext>().Database;
        var connectionString = database.GetConnectionString();
        var databasePath = connectionString?.Replace("Data Source=", string.Empty);
        DirectoryInfo parentDirectoryOfDatabase = Directory.GetParent(databasePath);
        if (!parentDirectoryOfDatabase.Exists)
        {
            Directory.CreateDirectory(parentDirectoryOfDatabase.FullName);
        }

        if (!File.Exists(databasePath))
        {
            database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB");
    }
}

void ConfigureShopAndEatServices(IServiceCollection services, IConfiguration configuration)
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
    services.AddTransient<IArticleDbAccess, ArticleDbAccess>();
    services.AddTransient<IGeneratePurchaseItemsForRecipesAction, GeneratePurchaseItemsForRecipesAction>();
    services.AddTransient<IOrderPurchaseItemsByStoreAction, OrderPurchaseItemsByStoreAction>();
    services.AddTransient<IGetRecipesForMealsAction, GetRecipesForMealsAction>();
    services.AddAutoMapper(config =>
    {
        config.AddProfile<AutoMapperProfile>();
        config.LicenseKey = "SomeInvalidLicenseKey"; // Replace with a valid license key if needed
    });

    services.AddDbContext<EfCoreContext>(options => options.UseLazyLoadingProxies().UseSqlite(configuration.GetConnectionString("SQLite")));

    services.AddHealthChecks().AddDbContextCheck<EfCoreContext>();
}