using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Features;
using Application.Common.Results;
using Application.DTOs.Notifications;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;

namespace Application.Features;

public class NotificationFeature(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    ISimpleSse backplane
) : INotificationFeature
{
    public async Task<Result> PokeUserAsync(Guid pokerId, Guid targetUserId)
    {
        // throws EntityNotFoundException if not found — caught in controller
        await userRepository.FindByIdAsync(targetUserId);

        var payload = JsonSerializer.Serialize(new { PokerId = pokerId });
        var notification = Notification.Create(targetUserId, NotificationType.Poke, payload);

        await notificationRepository.AddAsync(notification);

        await backplane.SendToUserAsync(targetUserId, new
        {
            type = "poke",
            notificationId = notification.Id,
            from = pokerId,
            createdAt = notification.CreatedAt
        });

        return Result.Success();
    }

    public async Task<Result<IEnumerable<NotificationDto>>> GetUnreadAsync(Guid userId)
    {
        var notifications = await notificationRepository.GetByUserIdAsync(userId, unreadOnly: true);
        return Result<IEnumerable<NotificationDto>>.Success(notifications.Select(NotificationDto.Map));
    }

    public async Task<Result> MarkReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await notificationRepository.FindByIdAsync(notificationId);

        if (notification.RecipientId != userId)
            return Result.Failure("Not your notification", ResultStatus.Unauthorized);

        await notificationRepository.MarkAsReadAsync(notificationId);
        return Result.Success();
    }
}