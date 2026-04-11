using Application.Features.Auth.Register;
using Application.Common.Results;
using Application.Services.FeatureFlags;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Utility;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Unit;

public class RegisterUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IHashingUtils _hashingUtils = Substitute.For<IHashingUtils>();
    private readonly RegisterUserHandler _handler;
    private readonly FeatureStateProvider _featureStateProvider = Substitute.For<FeatureStateProvider>();

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_userRepository, _hashingUtils, _featureStateProvider);
    }

    private static RegisterUserCommand MakeCommand(string email = "test@test.com") => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = email,
        Password = "Password1!",
        Role = RoleType.User
    };

    [Fact]
    public async Task HandleAsync_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        _userRepository.IsUserExistByEmailAsync(Arg.Any<string>()).Returns(false);
        _userRepository.AddAsync(Arg.Any<User>()).Returns(x => x.Arg<User>());

        var result = await _handler.HandleAsync(MakeCommand());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenEmailAlreadyInUse()
    {
        _userRepository.IsUserExistByEmailAsync(Arg.Any<string>()).Returns(true);

        var result = await _handler.HandleAsync(MakeCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("Email already in use.", result.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallCreatePasswordHash_BeforeAddingUser()
    {
        _userRepository.IsUserExistByEmailAsync(Arg.Any<string>()).Returns(false);
        _userRepository.AddAsync(Arg.Any<User>()).Returns(x => x.Arg<User>());

        await _handler.HandleAsync(MakeCommand());

        _hashingUtils.Received(1).CreatePasswordHash(
            Arg.Is("Password1!"),
            out Arg.Any<byte[]>()
        );
        
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFailure_WhenRepositoryThrows()
    {
        _userRepository.IsUserExistByEmailAsync(Arg.Any<string>()).Returns(false);
        _userRepository.AddAsync(Arg.Any<User>())
            .Throws(new RepositoryException("DB error"));

        var result = await _handler.HandleAsync(MakeCommand());

        Assert.False(result.IsSuccess);
        Assert.Contains("DB error", result.Message);
    }
}