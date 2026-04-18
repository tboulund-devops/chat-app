using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.Auth.Login;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Utility;
using Domain.Settings;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Unit;

public class LoginHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwt _jwt = Substitute.For<IJwt>();
    private readonly IHashingUtils _hashingUtils = Substitute.For<IHashingUtils>();
    private readonly LoginHandler _handler;

    private static readonly JwtSettings JwtSettings = new()
    {
        Secret = "supersecretkey_for_testing_32chars!",
        Issuer = "test",
        Audience = "test",
        AccessTokenLifetime = 15,
        RefreshTokenLifetime = 60
    };

    public LoginHandlerTests()
    {
        _handler = new LoginHandler(_userRepository, JwtSettings, _jwt, _hashingUtils);
    }

    private static User MakeUser() => User.Create(
        firstName: "John",
        lastName: "Doe",
        email: "john@doe.com",
        passwordHash: [1, 2, 3],
        role: RoleType.User
    );

    private static LoginCommand MakeCommand(string password = "correct") => new()
    {
        Email = "john@doe.com",
        Password = password
    };

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
    {
        var user = MakeUser();
        _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns(user);
        _hashingUtils.VerifyPasswordHash(Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);
        _jwt.GenerateToken(Arg.Any<Guid>()).Returns("access-token");
        _hashingUtils.GenerateRefreshToken().Returns("refresh-token");
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(true);

        var result = await _handler.HandleAsync(MakeCommand(), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Dto);
        Assert.Equal("access-token", result.Dto!.AccessToken);
        Assert.Equal("refresh-token", result.Dto.RefreshToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        var user = MakeUser();
        _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns(user);
        _hashingUtils.VerifyPasswordHash(Arg.Any<string>(), Arg.Any<byte[]>()).Returns(false);

        var result = await _handler.HandleAsync(MakeCommand("wrong"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthorized_WhenUserNotFound()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>())
            .Throws(new EntityNotFoundException("User not found"));

        var result = await _handler.HandleAsync(MakeCommand(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenRepositoryThrows()
    {
        _userRepository.GetByEmailAsync(Arg.Any<string>())
            .Throws(new RepositoryException("DB down"));

        var result = await _handler.HandleAsync(MakeCommand(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ShouldUpdateUserWithRefreshToken_OnSuccess()
    {
        var user = MakeUser();
        _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns(user);
        _hashingUtils.VerifyPasswordHash(Arg.Any<string>(), Arg.Any<byte[]>()).Returns(true);
        _jwt.GenerateToken(Arg.Any<Guid>()).Returns("token");
        _hashingUtils.GenerateRefreshToken().Returns("refresh");
        _userRepository.UpdateAsync(Arg.Any<User>()).Returns(true);

        await _handler.HandleAsync(MakeCommand(), TestContext.Current.CancellationToken);

        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u => u.RefreshToken == "refresh"));
    }
}