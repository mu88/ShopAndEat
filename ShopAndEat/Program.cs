using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using DataLayer.EF;
using DataLayer.EfClasses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ShopAndEat;

[ExcludeFromCodeCoverage]
public class Program
{
    public static void Main(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            System.IO.File.Copy("mu88_root_CA.crt", "/usr/local/share/ca-certificates/mu88_root_CA.crt", true);
                
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "update-ca-certificates",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                }
            };
            process.Start();
            var standardOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine(standardOutput);
        }

        var host = CreateHostBuilder(args).Build();

        CreateDbIfNotExists(host);

        host.Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, configuration) => configuration
                                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                           .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                                           .AddEnvironmentVariables())
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

    private static void CreateDbIfNotExists(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<EfCoreContext>();
            context.Database.EnsureCreated();
            // DbInitializer.Initialize(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred creating the DB.");
        }
    }

    private static class DbInitializer
    {
        public static void Initialize(EfCoreContext context)
        {
            context.Database.EnsureCreated();

            var mealType1 = new MealType("Mittagessen",20);
            var mealType2 = new MealType("Abendessen",30);
            context.MealTypes.Add(mealType1);
            context.MealTypes.Add(mealType2);
            context.SaveChanges();

            var unit1 = new Unit("Flasche");
            var unit2 = new Unit("Packung");
            var unit3 = new Unit("Gramm");
            var unit4 = new Unit("Milliliter");
            var unit5 = new Unit("Stück");
            context.Units.AddRange(unit1,unit2,unit3,unit4,unit5);
            context.SaveChanges();

            var articleGroup1 = new ArticleGroup("Wein & Spirituosen");
            var articleGroup2 = new ArticleGroup("Brot");
            var articleGroup3 = new ArticleGroup("Eier & gekühlte Fertigprodukte");
            var articleGroup4 = new ArticleGroup("Milchprodukte");
            var articleGroup5 = new ArticleGroup("Käse");
            var articleGroup6 = new ArticleGroup("Fleisch & Wurst");
            var articleGroup7 = new ArticleGroup("Fisch");
            var articleGroup8 = new ArticleGroup("Obst & Gemüse");
            var articleGroup9 = new ArticleGroup("Getränke");
            var articleGroup10 = new ArticleGroup("Tiefkühlprodukte");
            var articleGroup11 = new ArticleGroup("Kaffee & süße Aufstriche");
            var articleGroup12 = new ArticleGroup("Cerealien & Backwaren");
            var articleGroup13 = new ArticleGroup("Knabberzeugs & fremdländische Spezialitäten");
            var articleGroup14 = new ArticleGroup("Pasta, Reis & Rösti");
            var articleGroup15 = new ArticleGroup("Essig & Öl");
            var articleGroup16 = new ArticleGroup("Gewürze");
            var articleGroup17 = new ArticleGroup("Konserven");
            var articleGroup18 = new ArticleGroup("Haushaltsmittel");
            var articleGroup19 = new ArticleGroup("Hygieneprodukte");
            context.ArticleGroups.AddRange(articleGroup1, 
                                           articleGroup2,
                                           articleGroup3,
                                           articleGroup4,
                                           articleGroup5,
                                           articleGroup6,
                                           articleGroup7,
                                           articleGroup8,
                                           articleGroup9,
                                           articleGroup10,
                                           articleGroup11,
                                           articleGroup12,
                                           articleGroup13,
                                           articleGroup14,
                                           articleGroup15,
                                           articleGroup16,
                                           articleGroup17,
                                           articleGroup18,
                                           articleGroup19
                                          );
            context.SaveChanges();

            // var tomato = new Article("Tomato", articleGroup1, false);
            // var pasta = new Article("Pasta", articleGroup2, true);
            // context.Articles.AddRange(tomato, pasta);
            // context.SaveChanges();

            // var tomatoIngredient = new Ingredient(tomato, 2, piece);
            // var pastaIngredient = new Ingredient(pasta, 1, pack);
            // context.Ingredients.AddRange(tomatoIngredient, pastaIngredient);
            // context.SaveChanges();

            // var soup = new Recipe("Soup", 2, new[] { tomatoIngredient });
            // var pastaRecipe = new Recipe("Pasta", 1, new[] { pastaIngredient });
            // context.Recipes.AddRange(soup, pastaRecipe);
            // context.SaveChanges();

            // var meal1 = new Meal(DateTime.Now, mealType1, soup);
            // var meal2 = new Meal(DateTime.Now, mealType2, pastaRecipe);
            // var meal3 = new Meal(new DateTime(2020, 4, 12), mealType2, pastaRecipe);
            // context.Meals.AddRange(meal1, meal2, meal3);
            // context.SaveChanges();

            var shoppingOrder1 = new ShoppingOrder(articleGroup1, 0);
            var shoppingOrder2 = new ShoppingOrder(articleGroup2, 5);
            var shoppingOrder3 = new ShoppingOrder(articleGroup3, 10);
            var shoppingOrder4 = new ShoppingOrder(articleGroup4, 15);
            var shoppingOrder5 = new ShoppingOrder(articleGroup5, 20);
            var shoppingOrder6 = new ShoppingOrder(articleGroup6, 25);
            var shoppingOrder7 = new ShoppingOrder(articleGroup7, 30);
            var shoppingOrder8 = new ShoppingOrder(articleGroup8, 35);
            var shoppingOrder9 = new ShoppingOrder(articleGroup9, 40);
            var shoppingOrder10 = new ShoppingOrder(articleGroup10, 45);
            var shoppingOrder11 = new ShoppingOrder(articleGroup11, 50);
            var shoppingOrder12 = new ShoppingOrder(articleGroup12, 55);
            var shoppingOrder13 = new ShoppingOrder(articleGroup13, 60);
            var shoppingOrder14 = new ShoppingOrder(articleGroup14, 65);
            var shoppingOrder15 = new ShoppingOrder(articleGroup15, 70);
            var shoppingOrder16 = new ShoppingOrder(articleGroup16, 75);
            var shoppingOrder17 = new ShoppingOrder(articleGroup17, 80);
            var shoppingOrder18 = new ShoppingOrder(articleGroup18, 85);
            var shoppingOrder19 = new ShoppingOrder(articleGroup19, 90);
            context.ShoppingOrders.AddRange(shoppingOrder1, 
                                            shoppingOrder2,
                                            shoppingOrder3,
                                            shoppingOrder4,
                                            shoppingOrder5,
                                            shoppingOrder6,
                                            shoppingOrder7,
                                            shoppingOrder8,
                                            shoppingOrder9,
                                            shoppingOrder10,
                                            shoppingOrder11,
                                            shoppingOrder12,
                                            shoppingOrder13,
                                            shoppingOrder14,
                                            shoppingOrder15,
                                            shoppingOrder16,
                                            shoppingOrder17,
                                            shoppingOrder18,
                                            shoppingOrder19
                                           );
            context.SaveChanges();

            context.Stores.Add(new Store("Küsnacht", new[] { shoppingOrder1, 
                                             shoppingOrder2,
                                             shoppingOrder3,
                                             shoppingOrder4,
                                             shoppingOrder5,
                                             shoppingOrder6,
                                             shoppingOrder7,
                                             shoppingOrder8,
                                             shoppingOrder9,
                                             shoppingOrder10,
                                             shoppingOrder11,
                                             shoppingOrder12,
                                             shoppingOrder13,
                                             shoppingOrder14,
                                             shoppingOrder15,
                                             shoppingOrder16,
                                             shoppingOrder17,
                                             shoppingOrder18,
                                             shoppingOrder19 }));
            context.SaveChanges();
        }
    }
}