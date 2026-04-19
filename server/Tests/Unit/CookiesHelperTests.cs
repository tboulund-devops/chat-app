using Infrastructure.Utils;

namespace Unit;

public class CookieHelperTests
{
    [Fact]
    public void CreateAccessTokenCookieOptions_ShouldUseMinutesAsMaxAge()
    {
        var options = CookieHelper.CreateAccessTokenCookieOptions(10);

        Assert.Equal(TimeSpan.FromMinutes(10), options.MaxAge);
        Assert.True(options.HttpOnly);
        Assert.Equal("/", options.Path);
    }

    [Fact]
    public void CreateRefreshTokenCookieOptions_ShouldUseDays_WhenNotDevelopmentEnvironment()
    {
        var original = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
            var options = CookieHelper.CreateRefreshTokenCookieOptions(7);

            Assert.Equal(TimeSpan.FromDays(7), options.MaxAge);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", original);
        }
    }

    [Fact]
    public void CreateRefreshTokenCookieOptions_ShouldUseMinutes_WhenDevelopmentEnvironment()
    {
        var original = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var options = CookieHelper.CreateRefreshTokenCookieOptions(10);

            Assert.Equal(TimeSpan.FromMinutes(10), options.MaxAge);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", original);
        }
    }

    [Fact]
    public void CreateExpiredCookieOptions_ShouldReturnZeroMaxAge()
    {
        var options = CookieHelper.CreateExpiredCookieOptions();

        Assert.Equal(TimeSpan.Zero, options.MaxAge);
    }
}
