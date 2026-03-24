using Domain.Entities;

namespace Application.Common.Interfaces.Services;

public interface IUserService
{
    Task<User> GetUserByIdAsync(Guid id);
}