using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Features;
using Application.Common.Interfaces.Services;
using Application.Common.Results;
using Application.DTOs.Notifications;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;

namespace Application.Features.Notifications;

public class NotificationFeature(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    INotificationService notificationService
) : INotificationFeature
{
    public async Task<Result> PokeUserAsync(Guid pokerId, Guid targetUserId)
    {
        try
        {
            await userRepository.FindByIdAsync(targetUserId); // validate target exists
            await notificationService.NotifyPokeAsync(pokerId, targetUserId);
            return Result.Success();
        }
        catch (EntityNotFoundException ex)
        {
            return Result.Failure(ex.Message, ResultStatus.NotFound);
        }
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

    public async Task<Result> MarkAllReadAsync(Guid userId)
    {
        await notificationRepository.MarkAllAsReadAsync(userId);
        return Result.Success();
    }
}