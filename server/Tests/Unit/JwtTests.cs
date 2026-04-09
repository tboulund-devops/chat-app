using System.Security.Authentication;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Utility;
using Domain.Settings;
using Infrastructure.Auth;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Unit;

public class JwtTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IHashingUtils _hashingUtils = Substitute.For<IHashingUtils>();
    private readonly Jwt _jwt;

    private static readonly JwtSettings Settings = new()
    {
        Secret = "supersecretkey_for_testing_32chars!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenLifetime = 15,
        RefreshTokenLifetime = 60
    };

    public JwtTests()
    {
        _jwt = new Jwt(Settings, _userRepository, _hashingUtils);
    }

    private static User MakeUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        FirstName = "John",
        LastName = "Doe",
        Email = "john@doe.com",
        PasswordHash = [1, 2, 3],
        Role = RoleType.User,
        Activated = true,
        DateOfBirth = DateOnly.FromDateTime(new DateTime(1995, 5, 15))
    };

    // ── GenerateToken ────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateToken_ShouldReturnJwtString_WhenUserExists()
    {
        var user = MakeUser();
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        var token = await _jwt.GenerateToken(user.Id);

        Assert.NotNull(token);
        Assert.Contains(".", token); // JWT tokens have dots
        Assert.Equal(3, token.Split('.').Length); // Header.Payload.Signature
    }

    [Fact]
    public async Task GenerateToken_ShouldThrowArgumentException_WhenUserNotFound()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>())
            .Throws(new RepositoryException("User not found"));

        await Assert.ThrowsAsync<ArgumentException>(() => _jwt.GenerateToken(Guid.NewGuid()));
    }

    // ── RefreshTokenAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewAccessToken_WithExistingRefreshToken_WhenNotExpiringSoon()
    {
        var user = MakeUser();
        user.RefreshToken = "valid-refresh-token";
        user.RefreshTokenExpires = DateTime.UtcNow.AddMinutes(50); // lots of time left
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        var (accessToken, refreshToken) = await _jwt.RefreshTokenAsync("valid-refresh-token", user.Id);

        Assert.NotNull(accessToken);
        Assert.Equal("valid-refresh-token", refreshToken); // not rotated
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRotateRefreshToken_WhenLessThan25PercentLifetimeRemains()
    {
        var user = MakeUser();
        user.RefreshToken = "old-token";
        // 60 min lifetime, 25% = 15 min. Set to 10 min remaining → should rotate.
        user.RefreshTokenExpires = DateTime.UtcNow.AddMinutes(10);
        _userRepository.FindByIdAsync(user.Id).Returns(user);
        _hashingUtils.GenerateRefreshToken().Returns("new-refresh-token");
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(true);

        var (accessToken, refreshToken) = await _jwt.RefreshTokenAsync("old-token", user.Id);

        Assert.NotNull(accessToken);
        Assert.Equal("new-refresh-token", refreshToken);
        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u => u.RefreshToken == "new-refresh-token"));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowAuthenticationException_WhenTokenDoesNotMatch()
    {
        var user = MakeUser();
        user.RefreshToken = "correct-token";
        user.RefreshTokenExpires = DateTime.UtcNow.AddMinutes(30);
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            _jwt.RefreshTokenAsync("wrong-token", user.Id));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowAuthenticationException_WhenTokenIsExpired()
    {
        var user = MakeUser();
        user.RefreshToken = "expired-token";
        user.RefreshTokenExpires = DateTime.UtcNow.AddMinutes(-5); // already expired
        _userRepository.FindByIdAsync(user.Id).Returns(user);

        await Assert.ThrowsAsync<AuthenticationException>(() =>
            _jwt.RefreshTokenAsync("expired-token", user.Id));
    }
}
