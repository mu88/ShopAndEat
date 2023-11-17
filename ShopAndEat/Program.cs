using System.Diagnostics;
using System.Runtime.InteropServices;
using BizDbAccess;
using BizDbAccess.Concrete;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using DTO;
using Microsoft.EntityFrameworkCore;
using ServiceLayer;
using ServiceLayer.Concrete;

var builder = WebApplication.CreateBuilder(args);

ConfigureShopAndEatServices(builder.Services, builder.Configuration);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

InstallCertificate(app);
CreateDbIfNotExists(app);

app.UsePathBase("/shopAndEat");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.UseRouting();
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

void CreateDbIfNotExists(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        services.GetRequiredService<EfCoreContext>().Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB");
    }
}

void InstallCertificate(WebApplication webApp)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !webApp.Environment.IsDevelopment())
    {
        // File.Copy("mu88_root_CA.crt", "/usr/local/share/ca-certificates/mu88_root_CA.crt", true);
        //
        // var process = new Process
        // {
        //     StartInfo = new ProcessStartInfo { FileName = "update-ca-certificates", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = false, }
        // };
        // process.Start();
        // var standardOutput = process.StandardOutput.ReadToEnd();
        // process.WaitForExit();
        //
        // Console.WriteLine(standardOutput);
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
    services.AddAutoMapper(typeof(AutoMapperProfile));
    
    services.AddDbContext<EfCoreContext>(options => options.UseLazyLoadingProxies().UseSqlite(configuration.GetConnectionString("SQLite")));
}