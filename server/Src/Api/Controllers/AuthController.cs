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

        if (loginResult.Status != ResultStatus.Success)
        {
            return loginResult.Status switch
            {
                ResultStatus.Unauthorized => Unauthorized(),
                ResultStatus.Failure => BadRequest(),
                _ => BadRequest()
            };
        }
        // Only append cookies on success, when Dto is guaranteed non-null
        var cookieOptionsAccess = CookieHelper.CreateAccessTokenCookieOptions(appSettings.JwtSettings.AccessTokenLifetime);
        var cookieOptionsRefresh = CookieHelper.CreateRefreshTokenCookieOptions(appSettings.JwtSettings.RefreshTokenLifetime);

        Response.Cookies.Append("accessToken", loginResult.Dto!.AccessToken, cookieOptionsAccess);
        Response.Cookies.Append("refreshToken", loginResult.Dto!.RefreshToken!, cookieOptionsRefresh);
        
        
        return loginResult.Status switch
        {
            ResultStatus.Unauthorized => Unauthorized(),
            ResultStatus.Failure => BadRequest(),
            ResultStatus.Success => Ok(loginResult.Dto.User),
            // ResultStatus.Forbidden => BadRequest(loginResponseDto),
            // ResultStatus.NotFound => BadRequest(loginResponseDto),
            // _ => throw new ArgumentOutOfRangeException()
        };
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand registerRequest)
    {
        if (User.Identity is { IsAuthenticated: false } || User.Identity is { IsAuthenticated: true } && User.IsInRole(nameof(RoleType.User)))
        {
            if (registerRequest.Role is RoleType.Admin or RoleType.Crew)
            {
                return Unauthorized("You are not authorized to register this user.");
            } 
            var registerResult = await authFeature.HandleRegisterUser(registerRequest);
            if (registerResult.IsSuccess)
            {
                return Ok(registerResult);
            }
        }

        if (User.IsInRole(nameof(RoleType.Crew)))
        {
            if (registerRequest.Role is RoleType.Admin or RoleType.User)
            {
                return Unauthorized("You are not authorized to register this user.");
            }
            
            var registerResult = await authFeature.HandleRegisterUser(registerRequest);
            if (registerResult.IsSuccess)
            {
                return Ok(registerResult);
            }
        }

        if (User.IsInRole(nameof(RoleType.Admin)))
        {
            var registerResult = await authFeature.HandleRegisterUser(registerRequest);
            if (registerResult.IsSuccess)
            {
                return Ok(registerResult);
            }
        }
        
        return BadRequest();
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