using Application.Common.Interfaces;
using Application.Common.Interfaces.Features;
using Application.Common.Results;
using Application.DTOs.Entities;
using Application.DTOs.Responses;
using Application.Features.Auth.Login;
using Application.Features.Auth.Register;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;


namespace Application.Features.Auth;

public class AuthFeature(LoginHandler loginHandler, RegisterUserHandler registerUserHandler, IUserRepository userRepository) : IAuthFeature
{

    public Task<Result<LoginResponseDto>> HandleLogin(LoginCommand command)
    {
        return loginHandler.HandleAsync(command);
    }

    public Task<Result> HandleRegisterUser(RegisterUserCommand command)
    {
        return registerUserHandler.HandleAsync(command);
    }

    public async Task<Result<UserDto>> HandleMeRequest(Guid myId)
    {
        try
        {
            var user = await userRepository.FindByIdAsync(myId);
            return Result<UserDto>.Success(new UserDto
            {
                Email = user.Email,
                Username = $"{user.FirstName} {user.LastName}"
            }, message: "Successfully fetched 'me' user");
        }
        catch (EntityNotFoundException e)
        {
            return Result<UserDto>.Failure(e.Message);
        }
    }

    public async Task<Result> RevokeRefreshToken(Guid userId)
    {
        try
        {
            var user = await userRepository.FindByIdAsync(userId);
            user.RefreshToken = "";
            user.RefreshTokenExpires = DateTime.UtcNow;
            var isSuccess = await userRepository.UpdateAsync(user);
            return isSuccess ? Result.Success() : Result.Failure("Failed to revoke refresh token", ResultStatus.Failure);
        }
        catch (EntityNotFoundException e)
        {
            return Result.Failure(e.Message, ResultStatus.NotFound);
        }
    }
}