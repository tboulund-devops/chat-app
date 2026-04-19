using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Integration.Fixtures;

namespace Integration.RepositoryTests;

[Collection("Repository")]
public sealed class ChatRoomRepositoryTests(MyDbContextFixture fixture) : IAsyncLifetime
{
    private MyDbContext _context = null!;
    private ChatRoomRepository _repo = null!;
    private User _user = null!;

    public async ValueTask InitializeAsync()
    {
        _context = fixture.CreateDbContext();
        _repo = new ChatRoomRepository(_context);

        _user = User.Create("Room", "Owner", $"owner{Guid.NewGuid()}@test.com", [1, 2, 3], RoleType.User);
        await _context.Users.AddAsync(_user);
        await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _context.ChatRoomMembers.RemoveRange(_context.ChatRoomMembers);
        _context.ChatRooms.RemoveRange(_context.ChatRooms);
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
        await _context.DisposeAsync();
    }

    private ChatRoom MakeRoom(string name = "Test Room") =>
        ChatRoom.Create(name, _user.Id);

    [Fact]
    public async Task AddAsync_ShouldPersistRoom()
    {
        var room = await _repo.AddAsync(MakeRoom());
        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.NotNull(await _context.ChatRooms.FindAsync([room.Id], TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindByIdAsync_ShouldReturnRoom_WhenExists()
    {
        var room = await _repo.AddAsync(MakeRoom());
        var result = await _repo.FindByIdAsync(room.Id);
        Assert.Equal(room.Id, result.Id);
    }

    [Fact]
    public async Task FindByIdAsync_ShouldThrow_WhenNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _repo.FindByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_ByEntity_ShouldRemoveRoom()
    {
        var room = await _repo.AddAsync(MakeRoom());
        var result = await _repo.DeleteAsync(room);
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldReturnTrue_WhenExists()
    {
        var room = await _repo.AddAsync(MakeRoom());
        var result = await _repo.DeleteAsync(room.Id);
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ById_ShouldReturnFalse_WhenNotFound()
    {
        var result = await _repo.DeleteAsync(Guid.NewGuid());
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateRoom()
    {
        var room = await _repo.AddAsync(MakeRoom());
        var updated = room with { Name = "Updated Name" };
        var result = await _repo.UpdateAsync(updated);
        Assert.True(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnRooms()
    {
        await _repo.AddAsync(MakeRoom("Room A"));
        await _repo.AddAsync(MakeRoom("Room B"));
        var all = await _repo.GetAllAsync();
        Assert.True(all.Count() >= 2);
    }

    [Fact]
    public async Task GetAllRoomsAsync_ShouldExcludeDeleted()
    {
        var room = await _repo.AddAsync(MakeRoom("To Delete"));
        room = room with { IsDeleted = true };
        await _repo.UpdateAsync(room);

        var all = await _repo.GetAllRoomsAsync();
        Assert.DoesNotContain(all, r => r.Id == room.Id);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldAddMemberToRoom()
    {
        var room = await _repo.AddAsync(MakeRoom());
        var result = await _repo.AddMemberAsync(room.Id, _user.Id);
        Assert.True(result);
        Assert.True(await _repo.IsMemberAsync(room.Id, _user.Id));
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldRemoveMember_WhenExists()
    {
        var room = await _repo.AddAsync(MakeRoom());
        await _repo.AddMemberAsync(room.Id, _user.Id);
        var result = await _repo.RemoveMemberAsync(room.Id, _user.Id);
        Assert.True(result);
        Assert.False(await _repo.IsMemberAsync(room.Id, _user.Id));
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldReturnFalse_WhenMemberNotFound()
    {
        var room = await _repo.AddAsync(MakeRoom());
        var result = await _repo.RemoveMemberAsync(room.Id, Guid.NewGuid());
        Assert.False(result);
    }

    [Fact]
    public async Task IsMemberAsync_ShouldReturnFalse_WhenNotMember()
    {
        var room = await _repo.AddAsync(MakeRoom());
        Assert.False(await _repo.IsMemberAsync(room.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetRoomsForUserAsync_ShouldReturnUserRooms()
    {
        var room = await _repo.AddAsync(MakeRoom());
        await _repo.AddMemberAsync(room.Id, _user.Id);
        var rooms = await _repo.GetRoomsForUserAsync(_user.Id);
        Assert.Contains(rooms, r => r.Id == room.Id);
    }

    [Fact]
    public async Task GetByIdWithMembersAsync_ShouldReturnRoomWithMembers()
    {
        var room = await _repo.AddAsync(MakeRoom());
        await _repo.AddMemberAsync(room.Id, _user.Id);
        var result = await _repo.GetByIdWithMembersAsync(room.Id);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Members);
    }

    [Fact]
    public async Task GetByIdWithMembersAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _repo.GetByIdWithMembersAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchRoomsByNameAsync_ShouldReturnMatchingRooms()
    {
        await _repo.AddAsync(MakeRoom($"Searchable{Guid.NewGuid()}"));
        var results = await _repo.SearchRoomsByNameAsync("Searchable");
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task SearchRoomsByNameAsync_ShouldReturnEmpty_WhenNameIsWhitespace()
    {
        var results = await _repo.SearchRoomsByNameAsync("   ");
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetMembersAsync_ShouldReturnMembers()
    {
        var room = await _repo.AddAsync(MakeRoom());
        await _repo.AddMemberAsync(room.Id, _user.Id);
        var members = await _repo.GetMembersAsync(room.Id);
        Assert.NotEmpty(members);
    }
}