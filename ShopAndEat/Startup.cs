using System.Diagnostics.CodeAnalysis;
using BizDbAccess;
using BizDbAccess.Concrete;
using BizLogic;
using BizLogic.Concrete;
using DataLayer.EF;
using DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceLayer;
using ServiceLayer.Concrete;

namespace ShopAndEat;

[ExcludeFromCodeCoverage]
public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddServerSideBlazor();

        ConfigureShopAndEatServices(services);

        services.AddDbContext<EfCoreContext>(options => options.UseLazyLoadingProxies().UseSqlite(Configuration.GetConnectionString("SQLite")));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UsePathBase("/shopAndEat");
            
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
        });
    }

    private void ConfigureShopAndEatServices(IServiceCollection services)
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
    }
}