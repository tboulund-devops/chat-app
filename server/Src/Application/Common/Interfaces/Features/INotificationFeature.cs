using Application.Common.Results;
using Application.DTOs.Notifications;

namespace Application.Common.Interfaces.Features;

public interface INotificationFeature
{
    Task<Result> PokeUserAsync(Guid pokerId, Guid targetUserId);
    Task<Result<IEnumerable<NotificationDto>>> GetUnreadAsync(Guid userId);
    Task<Result> MarkReadAsync(Guid userId, Guid notificationId);
    Task<Result> MarkAllReadAsync(Guid userId);
}