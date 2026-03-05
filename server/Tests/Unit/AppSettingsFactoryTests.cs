using Api.Config;
using Microsoft.Extensions.Configuration;

namespace Unit;

public class AppSettingsFactoryTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    [Fact]
    public void Create_ShouldReturnAppSettings_WhenAllSectionsPresent()
    {
        var config = BuildConfig(new()
        {
            ["Jwt:Secret"] = "supersecretkey1234567890abcdef12",
            ["Jwt:Issuer"] = "myapp",
            ["Jwt:Audience"] = "myapp",
            ["Jwt:AccessTokenLifetime"] = "15",
            ["Jwt:RefreshTokenLifetime"] = "7",
            ["Database:PSqlConnectionString"] = "Host=localhost;Database=test",
            ["Database:RedisConnectionString"] = "localhost:6379",
            ["Cors:AllowedOrigins"] = "http://localhost:3000",
        });
        
        var result = AppSettingsFactory.Create(config);
        
        Assert.NotNull(result);
        Assert.NotNull(result.JwtSettings);
        Assert.NotNull(result.DbSettings);
        Assert.NotNull(result.CorsSettings);
    }
    
    [Fact]
    public void Create_ShouldThrow_WhenJwtSectionMissing()
    {
        var config = BuildConfig(new()
        {
            ["Database:PSqlConnectionString"] = "Host=localhost;Database=test",
            ["Database:RedisConnectionString"] = "localhost:6379",
            ["Cors:AllowedOrigins"] = "http://localhost:3000",
        });
        
        Assert.Throws<InvalidOperationException>(() => AppSettingsFactory.Create(config));
    }
    
    [Fact]
    public void Create_ShouldThrow_WhenDatabaseSectionMissing()
    {
        var config = BuildConfig(new()
        {
            ["Jwt:Secret"] = "supersecretkey1234567890abcdef12",
            ["Jwt:Issuer"] = "myapp",
            ["Jwt:Audience"] = "myapp",
            ["Cors:AllowedOrigins"] = "http://localhost:3000",
        });
        
        Assert.Throws<InvalidOperationException>(() => AppSettingsFactory.Create(config));   
    }
    
    [Fact]
    public void Create_ShouldThrow_WhenCorsSectionMissing()
    {
        var config = BuildConfig(new()
        {
            ["Jwt:Secret"] = "supersecretkey1234567890abcdef12",
            ["Jwt:Issuer"] = "myapp",
            ["Jwt:Audience"] = "myapp",
            ["Database:PSqlConnectionString"] = "Host=localhost;Database=test",
            ["Database:RedisConnectionString"] = "localhost:6379",
        });
        
        Assert.Throws<InvalidOperationException>(() => AppSettingsFactory.Create(config));  
    }
}