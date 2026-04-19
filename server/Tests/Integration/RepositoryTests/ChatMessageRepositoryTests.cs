using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Integration.Fixtures;

namespace Integration.RepositoryTests;

[Collection("Repository")]
public sealed class ChatMessageRepositoryTests(MyDbContextFixture fixture) : IAsyncLifetime
{
    private MyDbContext _context = null!;
    private ChatMessageRepository _repo = null!;
    private User _user = null!;
    private ChatRoom _room = null!;

    public async ValueTask InitializeAsync()
    {
        _context = fixture.CreateDbContext();
        _repo = new ChatMessageRepository(_context);

        _user = User.Create("Test", "User", $"msg{Guid.NewGuid()}@test.com", [1, 2, 3], RoleType.User);
        await _context.Users.AddAsync(_user);

        _room = ChatRoom.Create("Test Room", _user.Id);
        await _context.ChatRooms.AddAsync(_room);

        await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _context.ChatMessages.RemoveRange(_context.ChatMessages);
        _context.ChatRooms.RemoveRange(_context.ChatRooms);
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    private ChatMessage MakeMessage(string content = "Hello") =>
        ChatMessage.Create(_room.Id, _user.Id, content);

    [Fact]
    public async Task AddAsync_ShouldPersistMessage()
    {
        var msg = MakeMessage();
        var result = await _repo.AddAsync(msg);
        Assert.NotEqual(Guid.Empty, result.Id);
        var fromDb = await _context.ChatMessages.FindAsync([result.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
    }

    [Fact]
    public async Task FindByIdAsync_ShouldReturnMessage_WhenExists()
    {
        var msg = await _repo.AddAsync(MakeMessage());
        var result = await _repo.FindByIdAsync(msg.Id);
        Assert.Equal(msg.Id, result.Id);
    }

    [Fact]
    public async Task FindByIdAsync_ShouldThrow_WhenNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _repo.FindByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_ShouldRemoveMessage()
    {
        var msg = await _repo.AddAsync(MakeMessage());
        var result = await _repo.DeleteAsync(msg);
        Assert.True(result);
        Assert.Null(await _context.ChatMessages.FindAsync([msg.Id], TestContext.Current.CancellationToken));    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldReturnTrue_WhenExists()
    {
        var msg = await _repo.AddAsync(MakeMessage());
        var result = await _repo.DeleteAsync(msg.Id);
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldReturnFalse_WhenNotFound()
    {
        var result = await _repo.DeleteAsync(Guid.NewGuid());
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTrue_WhenMessageExists()
    {
        var msg = await _repo.AddAsync(MakeMessage());
        var updated = msg with { Content = "Updated" };
        var result = await _repo.UpdateAsync(updated);
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenMessageNotFound()
    {
        var fake = MakeMessage() with { Content = "Ghost" };
        var result = await _repo.UpdateAsync(fake);
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllMessages()
    {
        await _repo.AddAsync(MakeMessage("A"));
        await _repo.AddAsync(MakeMessage("B"));
        var all = await _repo.GetAllAsync();
        Assert.True(all.Count() >= 2);
    }

    [Fact]
    public async Task GetByRoomIdAsync_ShouldReturnMessagesForRoom()
    {
        await _repo.AddAsync(MakeMessage("Room msg"));
        var results = await _repo.GetByRoomIdAsync(_room.Id);
        Assert.NotEmpty(results);
        Assert.All(results, m => Assert.Equal(_room.Id, m.RoomId));
    }

    [Fact]
    public async Task GetByRoomIdAsync_ShouldRespectSkipAndTake()
    {
        for (var i = 0; i < 5; i++)
            await _repo.AddAsync(MakeMessage($"Msg {i}"));

        var results = await _repo.GetByRoomIdAsync(_room.Id, skip: 1, take: 2);
        Assert.Equal(2, results.Count());
    }

    [Fact]
    public async Task GetLatestByRoomIdAsync_ShouldReturnMessages()
    {
        await _repo.AddAsync(MakeMessage("Latest"));
        var results = await _repo.GetLatestByRoomIdAsync(_room.Id);
        Assert.NotEmpty(results);
    }
}