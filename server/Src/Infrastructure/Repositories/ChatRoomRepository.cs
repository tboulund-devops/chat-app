using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ChatRoomRepository(MyDbContext dbContext) : IChatRoomRepository
{
    public async Task<ChatRoom> AddAsync(ChatRoom entity)
    {
        try
        {
            var created = await dbContext.ChatRooms.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return created.Entity;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException(e.InnerException?.Message ?? e.Message, e);
        }
    }

    public async Task<bool> DeleteAsync(ChatRoom entity)
    {
        try
        {
            dbContext.ChatRooms.Remove(entity);
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException(e.Message, e);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var existing = dbContext.ChatRooms.FirstOrDefaultAsync(m => m.Id == id);
            if (existing == null)
                return false;
        
            dbContext.ChatRooms.Remove(await existing);
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException(e.Message, e);
        }    }

    public async Task<ChatRoom> FindByIdAsync(Guid id)
    {
        var room = await dbContext.ChatRooms
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        return room ?? throw new EntityNotFoundException("Chat room not found");
    }

    public async Task<IEnumerable<ChatRoom>> GetAllAsync()
    {
        return await dbContext.ChatRooms.ToListAsync();
    }

    public async Task<bool> UpdateAsync(ChatRoom entity)
    {
        try
        {
            var existing = await FindByIdAsync(entity.Id);
            entity = entity with { UpdatedAt = DateTime.UtcNow };
            dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException($"Failed to update chat room: {e.Message}", e);
        }
    }

    public async Task<ChatRoom?> GetByIdWithMembersAsync(Guid roomId)
    {
        return await dbContext.ChatRooms
            .Include(r => r.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == roomId && !r.IsDeleted);
    }

    public async Task<IEnumerable<ChatRoom>> GetRoomsForUserAsync(Guid userId)
    {
        return await dbContext.ChatRoomMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Room)
                .ThenInclude(r => r.Members)
            .Include(m => m.Room)
                .ThenInclude(r => r.CreatedBy)
            .Select(m => m.Room)
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> AddMemberAsync(Guid roomId, Guid userId)
    {
        try
        {
            var member = ChatRoomMember.Create(roomId, userId);
            await dbContext.ChatRoomMembers.AddAsync(member);
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException($"Failed to add member to room: {e.Message}", e);
        }
    }

    public async Task<bool> RemoveMemberAsync(Guid roomId, Guid userId)
    {
        try
        {
            var member = await dbContext.ChatRoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId);

            if (member != null)
            {
                dbContext.ChatRoomMembers.Remove(member);
                await dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException($"Failed to remove member from room: {e.Message}", e);
        }
    }

    public async Task<bool> IsMemberAsync(Guid roomId, Guid userId)
    {
        return await dbContext.ChatRoomMembers
            .AnyAsync(m => m.RoomId == roomId && m.UserId == userId);
    }

    public async Task<IEnumerable<ChatRoom>> SearchRoomsByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Enumerable.Empty<ChatRoom>();

        name = name.Trim();

        return await dbContext.ChatRooms
            .Where(r => !r.IsDeleted &&
                        r.Name != null &&
                        EF.Functions.ILike(r.Name, $"%{name}%")) // 🔥 case-insensitive
            .Include(r => r.Members)
            .Include(r => r.CreatedBy)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatRoom>> GetAllRoomsAsync()
    {
        return await dbContext.ChatRooms
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatRoomMember>> GetMembersAsync(Guid roomId)
    {
        return await dbContext.ChatRoomMembers
            .Where(m => m.RoomId == roomId)
            .Include(m => m.User)
            .ToListAsync();
    }
}
