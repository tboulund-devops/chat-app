using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Utility;

namespace Application.Features.Auth.Register;

public sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IHashingUtils hashingUtils
) : ICommandHandler<RegisterUserCommand, Result>
{
    public async Task<Result> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await userRepository.IsUserExistByEmailAsync(command.Email))
            {
                
                return Result.Failure("Email already in use.", ResultStatus.Failure);
            }
            
            hashingUtils.CreatePasswordHash(command.Password, out var passwordHash);

            var user = User.Create(
                command.FirstName,
                command.LastName,
                command.Email,
                passwordHash,
                command.Role
            );

            await userRepository.AddAsync(user);

            return Result.Success("User registered successfully.");
        }
        catch (RepositoryException e)
        {
            return Result.Failure(e.Message, ResultStatus.Failure);
        }
    }
}