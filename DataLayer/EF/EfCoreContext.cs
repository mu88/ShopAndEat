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

        ConfigureOnlineArticleMapping(modelBuilder);
        ConfigureShoppingPreference(modelBuilder);
        ConfigureShoppingSession(modelBuilder);
        ConfigureShoppingSessionItem(modelBuilder);
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
}
