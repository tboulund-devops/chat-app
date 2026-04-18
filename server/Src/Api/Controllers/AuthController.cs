using System.Security.Claims;
using Application.Common.Interfaces.Features;
using Application.Common.Results;
using Application.DTOs.Auth;
using Application.DTOs.Responses;
using Application.Features.Auth.Login;
using Application.Features.Auth.Register;
using Domain.Enums;
using Domain.Settings;
using Infrastructure.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthFeature authFeature, AppSettings appSettings) : BaseController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand loginRequest)
    {
        if (User.Identity is { IsAuthenticated: true })
            return NoContent();

        var loginResult = await authFeature.HandleLogin(loginRequest);

        if (!loginResult.IsSuccess)
            return loginResult.Status switch
            {
                ResultStatus.Unauthorized => Unauthorized(),
                ResultStatus.Failure => BadRequest(),
                _ => StatusCode(500)
            };

        var cookieOptionsAccess = CookieHelper.CreateAccessTokenCookieOptions(appSettings.JwtSettings.AccessTokenLifetime);
        var cookieOptionsRefresh = CookieHelper.CreateRefreshTokenCookieOptions(appSettings.JwtSettings.RefreshTokenLifetime);

        Response.Cookies.Append("accessToken", loginResult.Dto!.AccessToken, cookieOptionsAccess);
        Response.Cookies.Append("refreshToken", loginResult.Dto!.RefreshToken!, cookieOptionsRefresh);

        return Ok(loginResult.Dto.User);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand registerRequest)
    {
        var unauthorizedResult = GetUnauthorizedResult(registerRequest.Role);
        if (unauthorizedResult != null)
            return unauthorizedResult;

        var registerResult = await authFeature.HandleRegisterUser(registerRequest);
    
        return registerResult.IsSuccess
            ? Ok(registerResult)
            : BadRequest(registerResult.Message);
    }

    private UnauthorizedObjectResult? GetUnauthorizedResult(RoleType requestedRole)
    {
        if (User.IsInRole(nameof(RoleType.Admin)))
            return null;

        if (User.IsInRole(nameof(RoleType.Crew)))
            return requestedRole is RoleType.Admin or RoleType.User
                ? Unauthorized("You are not authorized to register this user.")
                : null;

        // Unauthenticated or plain User role
        return requestedRole is RoleType.Admin or RoleType.Crew
            ? Unauthorized("You are not authorized to register this user.")
            : null;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var meResult = await authFeature.HandleMeRequest(GetUserId());

            if (meResult.IsSuccess)
            {
                return Ok(meResult.Dto);
            }
        }
        catch (UnauthorizedAccessException e)
        {
            return Unauthorized(e.Message);
        }

        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");
            await authFeature.RevokeRefreshToken(GetUserId());
            return Ok();
        }
        catch (UnauthorizedAccessException e)
        {
            return Unauthorized(e.Message);
        }
    }
}