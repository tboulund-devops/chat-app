using System.Net.Http.Json;
using Application.Features.Auth.Login;
using Application.Features.Auth.Register;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration.Fixtures;

public static class AuthHelper
{
    private static int _counter = 0;

    public static async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync(ApiFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var id = Interlocked.Increment(ref _counter);
        var email = $"testuser{id}@test.com";
        const string password = "ValidPassword1!";

        var registerResponse = await client.PostAsJsonAsync("api/auth/register", new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = $"User{id}",
            Email = email,
            Password = password,
            Role = RoleType.User
        });

        if (!registerResponse.IsSuccessStatusCode)
        {
            var body = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Register failed {registerResponse.StatusCode}: {body}");
        }

        var loginResponse = await client.PostAsJsonAsync("api/auth/login", new LoginCommand
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var setCookieHeaders = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var accessTokenCookie = setCookieHeaders
                                    .FirstOrDefault(c => c.StartsWith("accessToken="))
                                ?? throw new InvalidOperationException("No accessToken cookie in login response");

        var tokenValue = accessTokenCookie.Split(';')[0];
        client.DefaultRequestHeaders.Add("Cookie", tokenValue);

        var meResponse = await client.GetAsync("api/auth/me");
        if (!meResponse.IsSuccessStatusCode)
        {
            var meBody = await meResponse.Content.ReadAsStringAsync();
            var cookiesSent = string.Join(" | ", setCookieHeaders);
            throw new InvalidOperationException(
                $"/me returned {meResponse.StatusCode}\n" +
                $"Token attached: {tokenValue[..Math.Min(60, tokenValue.Length)]}\n" +
                $"All Set-Cookie headers: {cookiesSent}\n" +
                $"/me body: {meBody}");
        }

        // Extract userId from JWT — no need for /me to return it
        var userId = ExtractUserIdFromJwt(tokenValue.Split('=', 2)[1]);

        return (client, userId);
    }

    private static Guid ExtractUserIdFromJwt(string token)
    {
        var payload = token.Split('.')[1];
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        var doc = System.Text.Json.JsonDocument.Parse(json);

        var idClaim = doc.RootElement
            .GetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
            .GetString();

        return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
    }
}