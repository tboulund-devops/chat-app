using System.Security.Authentication;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.DTOs.Auth;
using Application.DTOs.Entities;
using Application.DTOs.Responses;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Utility;
using Domain.Settings;

namespace Application.Features.Auth.Login;

public sealed class LoginHandler(
    IUserRepository userRepository,
    JwtSettings jwtSettings,
    IJwt jwt,
    IHashingUtils hashingUtils
    ) : ICommandHandler<LoginCommand, Result<LoginResponseDto>>
{
    public async Task<Result<LoginResponseDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userRepository.GetByEmailAsync(command.Email);

            if (!hashingUtils.VerifyPasswordHash(command.Password, user.PasswordHash))
                return Result<LoginResponseDto>.Failure("Invalid credentials.", ResultStatus.Unauthorized);

            var accessToken = await jwt.GenerateToken(user.Id);
            var refreshToken = hashingUtils.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpires = DateTime.UtcNow.AddMinutes(jwtSettings.RefreshTokenLifetime);
            await userRepository.UpdateAsync(user);
            var dto = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Email = user.Email,
                    Username = $"{user.FirstName} {user.LastName}"
                }
            };

            return Result<LoginResponseDto>.Success(dto);
        }
        catch (EntityNotFoundException)
        {
            return Result<LoginResponseDto>.Failure("Invalid credentials.", ResultStatus.Unauthorized);
        }
        catch (RepositoryException e)
        {
            return Result<LoginResponseDto>.Failure(e.Message);
        }
    }
}