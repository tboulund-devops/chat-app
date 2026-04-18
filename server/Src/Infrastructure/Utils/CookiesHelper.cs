using Domain.Interfaces.Utility;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Utils;

public abstract class CookieHelper()
{

    private static CookieOptions CreateCookieOptions(TimeSpan maxAge)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            MaxAge = maxAge,
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.None
        };
    }
    
    public static CookieOptions CreateAccessTokenCookieOptions(int expirationTime)
    {
        return CreateCookieOptions(TimeSpan.FromMinutes(expirationTime));
    }
    
    public static CookieOptions CreateRefreshTokenCookieOptions(int expirationTime)
    {
        var isDev = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        return CreateCookieOptions(isDev ? TimeSpan.FromMinutes(expirationTime) : TimeSpan.FromDays(expirationTime));
    }
    
    public static CookieOptions CreateExpiredCookieOptions()
    {
        return CreateCookieOptions(TimeSpan.Zero);
    }
}
