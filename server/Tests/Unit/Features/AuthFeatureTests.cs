using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.Common.Results;
using Application.DTOs.Responses;
using Application.Features.Auth;
using Application.Features.Auth.Login;
using Application.Features.Auth.Register;
using Application.Services.FeatureFlags;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Utility;
using Domain.Settings;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Unit;

public class AuthFeatureTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IFeatureStateProvider _featureStateProvider = Substitute.For<FeatureStateProvider>();
    
    private readonly AuthFeature _authFeature;

    public AuthFeatureTests()
    {
        var loginHandler = new LoginHandler(
            _userRepository,
            new JwtSettings
            {
                Secret = "test_secret_32chars_minimum_len!",
                Issuer = "test",
                Audience = "test",
                AccessTokenLifetime = 0,
                RefreshTokenLifetime = 0
            },
            Substitute.For<IJwt>(),
            Substitute.For<IHashingUtils>()
        );
        var registerHandler = new RegisterUserHandler(
            _userRepository,
            Substitute.For<IHashingUtils>(),
            _featureStateProvider
        );
        _authFeature = new AuthFeature(loginHandler, registerHandler, _userRepository);
    }

    private static User MakeUser() => User.Create("Jane", "Doe", "jane@doe.com", [1, 2, 3], RoleType.User);

    // ── HandleMeRequest ──────────────────────────────────────────────────

    [Fact]
    public async Task HandleMeRequest_ShouldReturnUserDto_WhenUserExists()
    {
        var user = MakeUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        var result = await _authFeature.HandleMeRequest(user.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Email, result.Dto!.Email);
        Assert.Equal($"{user.FirstName} {user.LastName}", result.Dto.Username);
    }

    [Fact]
    public async Task HandleMeRequest_ShouldReturnFailure_WhenUserNotFound()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>())
            .Throws(new EntityNotFoundException("User not found"));

        var result = await _authFeature.HandleMeRequest(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message);
    }

    // ── RevokeRefreshToken ───────────────────────────────────────────────

    [Fact]
    public async Task RevokeRefreshToken_ShouldReturnSuccess_WhenUserExistsAndUpdateSucceeds()
    {
        var user = MakeUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(true);

        var result = await _authFeature.RevokeRefreshToken(user.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RevokeRefreshToken_ShouldClearRefreshToken_OnUser()
    {
        var user = MakeUser();
        user.RefreshToken = "active-token";
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(true);

        await _authFeature.RevokeRefreshToken(user.Id);

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.RefreshToken == "" && u.RefreshTokenExpires <= DateTime.UtcNow)
        );
    }

    [Fact]
    public async Task RevokeRefreshToken_ShouldReturnFailure_WhenUpdateFails()
    {
        var user = MakeUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(false);

        var result = await _authFeature.RevokeRefreshToken(user.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("revoke", result.Message);
    }

    [Fact]
    public async Task RevokeRefreshToken_ShouldReturnFailure_WhenUserNotFound()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>())
            .Throws(new EntityNotFoundException("not found"));

        var result = await _authFeature.RevokeRefreshToken(Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }
}