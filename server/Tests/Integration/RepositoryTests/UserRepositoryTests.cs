using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Integration.Fixtures;

namespace Integration.RepositoryTests;

[Collection("Repository")]
public sealed class UserRepositoryTests(MyDbContextFixture fixture) : IAsyncLifetime
{
    private MyDbContext _context = null!;
    private UserRepository _userRepository = null!;

    public async ValueTask DisposeAsync()
    {
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    public ValueTask InitializeAsync()
    {
        _context = fixture.CreateDbContext();
        _userRepository = new UserRepository(_context);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ShouldSuccessfullyAddUserToRepository()
    {
        // Arrange
        var user = User.Create(
            firstName: "Johnny",
            lastName: "Green",
            email: "DonJohnny@wp.pl",
            role: RoleType.User,
            passwordHash: [1, 2, 3]
            );
        
        // Act 
        var result = await _userRepository.AddAsync(user);
        
        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(user.Id, result.Id);
        
        var fromDb = await _context.Users.FindAsync([result.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
        Assert.Equal("Johnny", fromDb.FirstName);
    }
    
}