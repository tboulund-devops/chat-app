using System.Security.Claims;
using Api.Controllers;
using Application.Common.Interfaces.Features;
using Application.Common.Results;
using Application.DTOs.Auth;
using Application.DTOs.Entities;
using Application.DTOs.Responses;
using Application.Features.Auth.Login;
using Application.Features.Auth.Register;
using Domain.Enums;
using Domain.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Unit;

public class AuthControllerTests
{
    private readonly IAuthFeature _authFeature = Substitute.For<IAuthFeature>();
    private readonly AppSettings _appSettings;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _appSettings = new AppSettings(
            new CorsSettings { AllowedOrigins = new[] { "https://localhost" } },
            new JwtSettings
            {
                Secret = new string('A', 32),
                Issuer = "issuer",
                Audience = "audience",
                AccessTokenLifetime = 15,
                RefreshTokenLifetime = 60
            },
            new DbSettings
            {
                PSqlConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
                RedisConnectionString = "localhost:6379"
            });

        _controller = new AuthController(_authFeature, _appSettings)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private void Authenticate(Guid userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    [Fact]
    public async Task Login_ShouldReturnNoContent_WhenUserIsAlreadyAuthenticated()
    {
        Authenticate(Guid.NewGuid());

        var result = await _controller.Login(new LoginCommand { Email = "test@example.com", Password = "password" });

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenLoginFailsWithUnauthorized()
    {
        _authFeature.HandleLogin(Arg.Any<LoginCommand>())
            .Returns(Result<LoginResponseDto>.Failure("invalid", ResultStatus.Unauthorized));

        var result = await _controller.Login(new LoginCommand { Email = "test@example.com", Password = "password" });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Login_ShouldReturnBadRequest_WhenLoginFailsWithFailure()
    {
        _authFeature.HandleLogin(Arg.Any<LoginCommand>())
            .Returns(Result<LoginResponseDto>.Failure("failure", ResultStatus.Failure));

        var result = await _controller.Login(new LoginCommand { Email = "test@example.com", Password = "password" });

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Login_ShouldReturnOkAndAppendCookies_WhenLoginSucceeds()
    {
        var userDto = new UserDto { Email = "user@example.com", Username = "User Name" };
        var responseDto = new LoginResponseDto
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            User = userDto
        };

        _authFeature.HandleLogin(Arg.Any<LoginCommand>())
            .Returns(Result<LoginResponseDto>.Success(responseDto));

        var result = await _controller.Login(new LoginCommand { Email = "test@example.com", Password = "password" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(userDto, ok.Value);
        var setCookieHeaders = _controller.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("accessToken=access-token", setCookieHeaders);
        Assert.Contains("refreshToken=refresh-token", setCookieHeaders);
    }

    [Fact]
    public async Task Register_ShouldReturnUnauthorized_WhenUserIsNotAllowedToRegisterAdmin()
    {
        Authenticate(Guid.NewGuid());

        var request = new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Password = "password",
            Role = RoleType.Admin
        };

        var result = await _controller.Register(request);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Contains("not authorized", unauthorized.Value!.ToString()!);
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenRegisterSucceeds()
    {
        var request = new RegisterUserCommand
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Password = "password",
            Role = RoleType.User
        };

        _authFeature.HandleRegisterUser(request)
            .Returns(Result.Success("ok"));

        var result = await _controller.Register(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<Result>(ok.Value);
    }

    [Fact]
    public async Task GetMe_ShouldReturnOk_WhenFeatureReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);

        var dto = new UserDto { Email = "me@example.com", Username = "Me" };
        _authFeature.HandleMeRequest(userId)
            .Returns(Result<UserDto>.Success(dto));

        var result = await _controller.GetMe();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenUserIdIsInvalid()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var result = await _controller.GetMe();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Logout_ShouldReturnOk_WhenRevokeRefreshTokenSucceeds()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);

        _authFeature.RevokeRefreshToken(userId)
            .Returns(Task.FromResult(Result.Success()));

        var result = await _controller.Logout();

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Logout_ShouldReturnUnauthorized_WhenGetUserIdThrows()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        var result = await _controller.Logout();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
