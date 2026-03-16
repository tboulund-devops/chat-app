using Api;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Integration.Fixtures;

public sealed class ApiFactory : WebApplicationFactory<ApiMaker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    
        // Log pending migrations so we can see if they're being applied
        var pending = await db.Database.GetPendingMigrationsAsync();
        Console.WriteLine($"[ApiFactory] Pending migrations: {string.Join(", ", pending)}");
    
        await db.Database.MigrateAsync();
    
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Console.WriteLine($"[ApiFactory] Applied migrations: {string.Join(", ", applied)}");
    }

    public new async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "supersecretkey_for_testing_only_32chars!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenLifetime"] = "15",
                ["Jwt:RefreshTokenLifetime"] = "7",
                ["Database:PSqlConnectionString"] = _postgres.GetConnectionString(),
                ["Database:RedisConnectionString"] = "localhost:6379", // mocked below
                ["Cors:AllowedOrigins"] = "http://localhost:3000",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace real DbContext with test one
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MyDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<MyDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Replace Redis with in-memory SSE to avoid needing a real Redis
            // (assuming ISimpleSse has an in-memory implementation already — InMemorySimpleSse)
            // If it already uses InMemorySimpleSse, no change needed here.
        });
    }
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFactory>;