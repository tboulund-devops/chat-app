using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ChatMessageRepository(MyDbContext dbContext) : IChatMessageRepository
{
    public async Task<ChatMessage> AddAsync(ChatMessage entity)
    {
        try
        {
            var created = await dbContext.ChatMessages.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return created.Entity;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException(e.Message, e);
        }
    }

    public async Task<bool> DeleteAsync(ChatMessage entity)
    {
        try
        {
            dbContext.ChatMessages.Remove(entity);
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
            var existing = await dbContext.ChatMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (existing == null)
                return false;
    
            dbContext.ChatMessages.Remove(existing);
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException(e.Message, e);
        }
    }

    public async Task<ChatMessage> FindByIdAsync(Guid id)
    {
        var message = await dbContext.ChatMessages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == id);

        return message ?? throw new EntityNotFoundException("Chat message not found");
    }

    public async Task<IEnumerable<ChatMessage>> GetAllAsync()
    {
        return await dbContext.ChatMessages.ToListAsync();
    }

    public async Task<bool> UpdateAsync(ChatMessage entity)
    {
        try
        {
            var existing = await dbContext.ChatMessages.FirstOrDefaultAsync(m => m.Id == entity.Id);
            if (existing == null)
                return false;
    
            dbContext.Entry(existing).CurrentValues.SetValues(entity);
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException e)
        {
            throw new RepositoryException($"Failed to update chat message: {e.Message}", e);
        }
    }

    public async Task<IEnumerable<ChatMessage>> GetByRoomIdAsync(Guid roomId, int skip = 0, int take = 50)
    {
        return await dbContext.ChatMessages
            .Where(m => m.RoomId == roomId && !m.IsDeleted)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatMessage>> GetLatestByRoomIdAsync(Guid roomId, int count = 50)
    {
        return await GetByRoomIdAsync(roomId, 0, count);
    }
}
