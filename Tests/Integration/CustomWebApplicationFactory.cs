using System.Data.Common;
using DataLayer.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
            services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbConnection));
            services.Remove(dbConnectionDescriptor);

            // Create open SqliteConnection so EF won't automatically close it.
            services.AddSingleton<DbConnection>(_ =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                return connection;
            });

            services.AddDbContext<EfCoreContext>((container, options) => options.UseLazyLoadingProxies().UseSqlite(container.GetRequiredService<DbConnection>()));
        });

        builder.UseEnvironment("Development");
    }
}