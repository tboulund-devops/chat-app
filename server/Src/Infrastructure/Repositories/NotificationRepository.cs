using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class NotificationRepository(MyDbContext dbContext) : INotificationRepository
{
    public async Task<Notification> AddAsync(Notification entity)
    {
        dbContext.Notifications.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(Notification entity) => await DeleteAsync(entity.Id);

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await dbContext.Notifications
            .Where(n => n.Id == id)
            .ExecuteDeleteAsync() > 0;
    }

    public async Task<Notification> FindByIdAsync(Guid id)
    {
        return await dbContext.Notifications.FindAsync(id)
               ?? throw new EntityNotFoundException($"Notification with id '{id}' not found");
    }

    public async Task<IEnumerable<Notification>> GetAllAsync() => await dbContext.Notifications.ToListAsync();

    public async Task<bool> UpdateAsync(Notification entity)
    {
        dbContext.Notifications.Update(entity);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, bool unreadOnly = false)
    {
        return await dbContext.Notifications
            .Where(n => n.RecipientId == userId && (!unreadOnly || !n.IsRead))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        return await dbContext.Notifications
            .Where(n => n.Id == notificationId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true)) > 0;
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await dbContext.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));       
    }
}