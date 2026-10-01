using DataLayer.EfClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DataLayer.EF;

public class EfCoreContext(DbContextOptions<EfCoreContext> options) : DbContext(options)
{
    public DbSet<ArticleGroup> ArticleGroups { get; set; }

    public DbSet<Article> Articles { get; set; }

    public DbSet<MealType> MealTypes { get; set; }

    public DbSet<Unit> Units { get; set; }

    public DbSet<Ingredient> Ingredients { get; set; }

    public DbSet<PurchaseItem> PurchaseItems { get; set; }

    public DbSet<Purchase> Purchases { get; set; }

    public DbSet<Recipe> Recipes { get; set; }

    public DbSet<Meal> Meals { get; set; }

    public DbSet<Store> Stores { get; set; }

    public DbSet<ShoppingOrder> ShoppingOrders { get; set; }

    public DbSet<OnlineArticleMapping> OnlineArticleMappings { get; set; }

    public DbSet<ShoppingPreference> ShoppingPreferences { get; set; }

    public DbSet<ShoppingSession> ShoppingSessions { get; set; }

    public DbSet<ShoppingSessionItem> ShoppingSessionItems { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToStringConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureArticle(modelBuilder);
        ConfigureArticleGroup(modelBuilder);
        ConfigureIngredient(modelBuilder);
        ConfigureMeal(modelBuilder);
        ConfigureOnlineArticleMapping(modelBuilder);
        ConfigurePurchaseItem(modelBuilder);
        ConfigureRecipe(modelBuilder);
        ConfigureShoppingOrder(modelBuilder);
        ConfigureShoppingPreference(modelBuilder);
        ConfigureShoppingSession(modelBuilder);
        ConfigureShoppingSessionItem(modelBuilder);
        ConfigureStore(modelBuilder);
        ConfigureUnit(modelBuilder);
    }

    private static void ConfigureArticle(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.Property(e => e.ArticleId).HasConversion(
                id => id.Value,
                value => new ArticleId(value))
                .ValueGeneratedOnAdd();

            // Deleting an ArticleGroup must not cascade-delete every Article referencing it; the
            // default EF Core behavior for this required relationship is Cascade, which would
            // silently wipe out articles. Require the group to be reassigned/emptied first.
            entity.HasOne(article => article.ArticleGroup)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureIngredient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            // Deleting an Article or Unit must not cascade-delete every Ingredient referencing
            // it; the default EF Core behavior for these required relationships is Cascade.
            entity.HasOne(ingredient => ingredient.Article)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ingredient => ingredient.Unit)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePurchaseItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            // Deleting an Article or Unit must not cascade-delete every PurchaseItem referencing
            // it; the default EF Core behavior for these required relationships is Cascade.
            entity.HasOne(purchaseItem => purchaseItem.Article)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(purchaseItem => purchaseItem.Unit)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureShoppingOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingOrder>(entity =>
        {
            // Deleting an ArticleGroup must not cascade-delete its ShoppingOrder entry; the
            // default EF Core behavior for this required relationship is Cascade.
            entity.HasOne(shoppingOrder => shoppingOrder.ArticleGroup)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureArticleGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleGroup>(entity =>
        {
            entity.Property(e => e.ArticleGroupId).HasConversion(
                id => id.Value,
                value => new ArticleGroupId(value))
                .ValueGeneratedOnAdd();
        });
    }

    private static void ConfigureMeal(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meal>(entity =>
        {
            entity.Property(e => e.MealId).HasConversion(
                id => id.Value,
                value => new MealId(value))
                .ValueGeneratedOnAdd();

            // Deleting a MealType must not cascade-delete every Meal referencing it; the default
            // EF Core behavior for this required relationship is Cascade. Meal -> Recipe is left
            // as Cascade on purpose: deleting a Recipe should delete the meals planned from it
            // (see DeleteRecipeAsync_WithRealDatabaseCascade_DeletesMealsViaDbConstraint).
            entity.HasOne(meal => meal.MealType)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRecipe(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(e => e.RecipeId).HasConversion(
                id => id.Value,
                value => new RecipeId(value))
                .ValueGeneratedOnAdd();
        });
    }

    private static void ConfigureOnlineArticleMapping(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OnlineArticleMapping>(entity =>
        {
            entity.Property(e => e.OnlineArticleMappingId).HasConversion(
                id => id.Value,
                value => new OnlineArticleMappingId(value))
                .ValueGeneratedOnAdd();

            entity.HasIndex(mapping => new { mapping.ArticleName, mapping.StoreKey, mapping.StoreProductCode }).IsUnique();
            entity.HasIndex(mapping => new { mapping.StoreKey, mapping.ArticleName });

            entity.Property(e => e.ArticleName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.StoreKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StoreProductCode).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StoreProductName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MatchMethod).HasMaxLength(50).IsRequired().HasConversion<string>();
        });
    }

    private static void ConfigureShoppingPreference(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingPreference>(entity =>
        {
            entity.Property(e => e.ShoppingPreferenceId).HasConversion(
                id => id.Value,
                value => new ShoppingPreferenceId(value))
                .ValueGeneratedOnAdd();

            entity.HasIndex(preference => new { preference.Scope, preference.Key, preference.StoreKey }).IsUnique();

            entity.Property(e => e.Scope).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Key).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(10000).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(100).HasConversion<string>();
            entity.Property(e => e.StoreKey).HasMaxLength(100);
        });
    }

    private static void ConfigureShoppingSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingSession>(entity =>
        {
            entity.Property(e => e.ShoppingSessionId).HasConversion(
                id => id.Value,
                value => new ShoppingSessionId(value))
                .ValueGeneratedOnAdd();

            entity.HasIndex(e => e.StartedAt);

            entity.Property(e => e.IngredientList).HasMaxLength(50000);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired().HasConversion<string>();
        });
    }

    private static void ConfigureShoppingSessionItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingSessionItem>(entity =>
        {
            entity.Property(e => e.ShoppingSessionItemId).HasConversion(
                id => id.Value,
                value => new ShoppingSessionItemId(value))
                .ValueGeneratedOnAdd();

            entity.Property(e => e.SessionId).HasColumnName("ShoppingSessionId").HasConversion(
                id => id.Value,
                value => new ShoppingSessionId(value));

            entity.HasOne(item => item.ShoppingSession)
                .WithMany(session => session.Items)
                .HasForeignKey(item => item.SessionId);

            entity.Property(e => e.OriginalIngredient).HasMaxLength(500).IsRequired();
            entity.Property(e => e.SelectedProductName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.SelectedProductUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.Price).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired().HasConversion<string>();
        });
    }

    private static void ConfigureStore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Store>(entity =>
        {
            entity.Property(e => e.StoreId).HasConversion(
                id => id.Value,
                value => new StoreId(value))
                .ValueGeneratedOnAdd();
        });
    }

    private static void ConfigureUnit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.Property(e => e.UnitId).HasConversion(
                id => id.Value,
                value => new UnitId(value))
                .ValueGeneratedOnAdd();
        });
    }
}
