using Application.Common.Interfaces.Services;
using Application.DTOs.Entities;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;

namespace Application.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<User> GetUserByIdAsync(Guid id)
    {
        return await userRepository.FindByIdAsync(id);
    }
}