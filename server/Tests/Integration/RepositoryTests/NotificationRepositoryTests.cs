using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Integration.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Integration.RepositoryTests;

[Collection("Repository")]
public sealed class NotificationRepositoryTests(MyDbContextFixture fixture) : IAsyncLifetime
{
    private MyDbContext _context = null!;
    private NotificationRepository _repo = null!;
    private User _user = null!;

    public async ValueTask InitializeAsync()
    {
        _context = fixture.CreateDbContext();
        _repo = new NotificationRepository(_context);

        _user = User.Create("Notif", "User", $"notif{Guid.NewGuid()}@test.com", [1, 2, 3], RoleType.User);
        await _context.Users.AddAsync(_user);
        await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.Notifications
            .Where(n => n.RecipientId == _user.Id)
            .ExecuteDeleteAsync();
        await _context.Users
            .Where(u => u.Id == _user.Id)
            .ExecuteDeleteAsync();
        await _context.DisposeAsync();
    }

    private Notification MakeNotification() =>
        Notification.Create(_user.Id, NotificationType.NewMessage, "{}");

    [Fact]
    public async Task AddAsync_ShouldPersistNotification()
    {
        var n = await _repo.AddAsync(MakeNotification());
        Assert.NotEqual(Guid.Empty, n.Id);
        Assert.NotNull(await _context.Notifications.FindAsync(n.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByIdAsync_ShouldReturnNotification_WhenExists()
    {
        var n = await _repo.AddAsync(MakeNotification());
        var result = await _repo.FindByIdAsync(n.Id);
        Assert.Equal(n.Id, result.Id);
    }

    [Fact]
    public async Task FindByIdAsync_ShouldThrow_WhenNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _repo.FindByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_ShouldDelete()
    {
        var n = await _repo.AddAsync(MakeNotification());
        var result = await _repo.DeleteAsync(n);
        Assert.True(result);
        var fromDb = await _context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == n.Id, TestContext.Current.CancellationToken);
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldReturnTrue_WhenExists()
    {
        var n = await _repo.AddAsync(MakeNotification());
        var result = await _repo.DeleteAsync(n.Id);
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldReturnFalse_WhenNotFound()
    {
        var result = await _repo.DeleteAsync(Guid.NewGuid());
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTrue_WhenUpdated()
    {
        var n = await _repo.AddAsync(MakeNotification());
        _context.Entry(n).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        var updated = n with { IsRead = true };
        var result = await _repo.UpdateAsync(updated);
        Assert.True(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnNotifications()
    {
        await _repo.AddAsync(MakeNotification());
        var all = await _repo.GetAllAsync();
        Assert.NotEmpty(all);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnAllNotifications_WhenUnreadOnlyFalse()
    {
        await _repo.AddAsync(MakeNotification());
        var results = await _repo.GetByUserIdAsync(_user.Id, unreadOnly: false);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUnread_WhenUnreadOnlyTrue()
    {
        var n = await _repo.AddAsync(MakeNotification());
        await _repo.MarkAsReadAsync(n.Id);
        await _repo.AddAsync(MakeNotification());

        var results = await _repo.GetByUserIdAsync(_user.Id, unreadOnly: true);
        Assert.All(results, r => Assert.False(r.IsRead));
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldReturnTrue_WhenExists()
    {
        var n = await _repo.AddAsync(MakeNotification());
        var result = await _repo.MarkAsReadAsync(n.Id);
        Assert.True(result);
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldReturnFalse_WhenNotFound()
    {
        var result = await _repo.MarkAsReadAsync(Guid.NewGuid());
        Assert.False(result);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_ShouldMarkAllUnreadNotifications()
    {
        await _repo.AddAsync(MakeNotification());
        await _repo.AddAsync(MakeNotification());
        await _repo.MarkAllAsReadAsync(_user.Id);

        var unread = await _repo.GetByUserIdAsync(_user.Id, unreadOnly: true);
        Assert.Empty(unread);
    }
}