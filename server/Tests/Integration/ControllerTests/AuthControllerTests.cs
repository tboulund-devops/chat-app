using System.Net;
using System.Net.Http.Json;
using Application.Features.Auth.Login;
using Application.Features.Auth.Register;
using Domain.Enums;
using Integration.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Integration.ControllerTests;

[Collection("Api")]
public sealed class AuthControllerTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = false  // don't parse Set-Cookie automatically
    });
    
    // ── Login ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ShouldReturn401_WhenCredentialsAreInvalid()
    {
        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginCommand
        {
            Email = "nobody@nowhere.com",
            Password = "wrongpassword"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturn200AndSetCookies_WhenCredentialsAreValid()
    {
        await _client.PostAsJsonAsync("api/auth/register", new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            Password = "ValidPassword1!",
            Role = RoleType.User
        });

        var response = await _client.PostAsJsonAsync("api/auth/login", new LoginCommand
        {
            Email = "test@test.com",
            Password = "ValidPassword1!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
        // Assert on raw header instead of parsed cookie container
        Assert.True(response.Headers.Contains("Set-Cookie"));
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.StartsWith("accessToken="));
        Assert.Contains(cookies, c => c.StartsWith("refreshToken="));
    }

    // ── Register ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ShouldReturn200_WhenRegisteringNormalUser_Unauthenticated()
    {
        var response = await _client.PostAsJsonAsync("api/auth/register", new RegisterUserCommand
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@doe.com",
            Password = "SecurePass1!",
            Role = RoleType.User
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturn401_WhenUnauthenticatedUserTriesToRegisterAdmin()
    {
        var response = await _client.PostAsJsonAsync("api/auth/register", new RegisterUserCommand
        {
            FirstName = "Bad",
            LastName = "Actor",
            Email = "badactor@evil.com",
            Password = "SecurePass1!",
            Role = RoleType.Admin
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Me ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Logout ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.PostAsync("api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}