using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;

namespace Application.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    IChatRoomRepository roomRepository,
    IUserRepository userRepository,
    ISimpleSse backplane
) : INotificationService
{
    // Triggered when a message is sent
    public async Task NotifyNewMessageAsync(Guid senderId, Guid roomId, Guid messageId, string content)
    {
        var members = await roomRepository.GetMembersAsync(roomId);
        var room = await roomRepository.FindByIdAsync(roomId);
        var sender = await userRepository.FindByIdAsync(senderId);
        var senderName = $"{sender.FirstName} {sender.LastName}";

        var tasks = members
            .Where(m => m.UserId != senderId)
            .Select(async m =>
            {
                var payload = JsonSerializer.Serialize(new
                {
                    roomId,
                    roomName = room.Name,
                    messageId,
                    senderName,
                    content
                });

                var notification = Notification.Create(m.UserId, NotificationType.NewMessage, payload);
                await notificationRepository.AddAsync(notification);
                await backplane.SendToUserAsync(m.UserId, new
                {
                    type = "new_message",
                    notificationId = notification.Id,
                    roomId,
                    roomName = room.Name,
                    senderId,
                    senderName,
                    content
                }, "notification");
            });

        await Task.WhenAll(tasks);
    }

    // Triggered when a user pokes another
    public async Task NotifyPokeAsync(Guid pokerId, Guid targetUserId)
    {
        var poker = await userRepository.FindByIdAsync(pokerId);
        var pokerName = $"{poker.FirstName} {poker.LastName}";

        var payload = JsonSerializer.Serialize(new { pokerId, pokerName });
        var notification = Notification.Create(targetUserId, NotificationType.Poke, payload);
        await notificationRepository.AddAsync(notification);

        await backplane.SendToUserAsync(targetUserId, new
        {
            type = "poke",
            notificationId = notification.Id,
            pokerId,
            pokerName,
            createdAt = notification.CreatedAt
        }, "notification");
    }

    // Future: NotifyUserJoinedAsync, NotifyMentionAsync, etc.
}