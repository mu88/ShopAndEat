using System.Data.Common;
using DataLayer.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration;

internal class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<EfCoreContext>));
            services.Remove(dbContextDescriptor!);

            var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            services.Remove(dbConnectionDescriptor!);

            // Create open SqliteConnection so EF won't automatically close it.
            services.AddSingleton<DbConnection>(_ =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                return connection;
            });

            // ConfigureWarnings: see matching comment in ShopAndEat/Program.cs — suppresses a
            // non-convergent dotnet-ef tooling false positive (Sqlite:Autoincrement annotation diff
            // for the value-converted strongly-typed primary keys), not a real pending model change.
            services.AddDbContext<EfCoreContext>((container, options) => options.UseLazyLoadingProxies()
                                                                                 .UseSqlite(container.GetRequiredService<DbConnection>())
                                                                                 .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
        });

        builder.UseEnvironment("Development");
    }
}
