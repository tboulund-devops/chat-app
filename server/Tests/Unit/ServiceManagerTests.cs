using Api.Config;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Features;
using Application.Common.Interfaces.Services;
using Domain.Settings;
using Infrastructure.Auth;
using Infrastructure.Sse;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using StackExchange.Redis;

namespace Unit;

public class ServiceManagerTests
{
    [Fact]
    public void ConfigureAndInitializeServices_ShouldRegisterRequiredServices()
    {
        var services = new ServiceCollection();
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);

        var appSettings = new AppSettings(
            new CorsSettings { AllowedOrigins = new[] { "https://localhost" } },
            new JwtSettings
            {
                Secret = new string('A', 32),
                Issuer = "issuer",
                Audience = "audience",
                AccessTokenLifetime = 60,
                RefreshTokenLifetime = 120
            },
            new DbSettings
            {
                PSqlConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
                RedisConnectionString = "localhost:6379"
            }
        );

        var manager = new ServiceManager(services, appSettings, env);
        manager.ConfigureAndInitializeServices();

        var serviceTypes = services.Select(d => d.ServiceType).ToHashSet();

        Assert.Contains(typeof(AppSettings), serviceTypes);
        Assert.Contains(typeof(DbSettings), serviceTypes);
        Assert.Contains(typeof(JwtSettings), serviceTypes);
        Assert.Contains(typeof(CorsSettings), serviceTypes);
        Assert.Contains(typeof(IJwt), serviceTypes);
        Assert.Contains(typeof(IChatFeature), serviceTypes);
        Assert.Contains(typeof(INotificationService), serviceTypes);
        Assert.Contains(typeof(IFeatureStateProvider), serviceTypes);
        Assert.Contains(typeof(ISimpleSse), serviceTypes);
        Assert.Contains(typeof(IConnectionMultiplexer), serviceTypes);
    }

    [Fact]
    public void ConfigureAndInitializeServices_ShouldThrow_WhenJwtSettingsAreInvalid()
    {
        var services = new ServiceCollection();
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);

        var appSettings = new AppSettings(
            new CorsSettings { AllowedOrigins = new[] { "https://localhost" } },
            new JwtSettings
            {
                Secret = "short",
                Issuer = "issuer",
                Audience = "audience",
                AccessTokenLifetime = 60,
                RefreshTokenLifetime = 120
            },
            new DbSettings
            {
                PSqlConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
                RedisConnectionString = "localhost:6379"
            }
        );

        var manager = new ServiceManager(services, appSettings, env);

        var exception = Assert.Throws<Domain.Exceptions.ConfigurationFailureException>(() => manager.ConfigureAndInitializeServices());
        Assert.Contains("JWT Secret", exception.Message);
    }
}
