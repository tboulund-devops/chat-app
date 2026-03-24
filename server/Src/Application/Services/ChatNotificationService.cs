using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;

namespace Application.Services;

public class ChatNotificationService(
    INotificationRepository notificationRepository,
    IChatRoomRepository roomRepository,
    ISimpleSse backplane
) : IChatNotificationService
{
    public async Task NotifyRoomMemberAsync(Guid userId, Guid requestRoomId, Guid messageId, string content)
    {
        var members = await roomRepository.GetMembersAsync(requestRoomId);
        var tasks = members
            .Where(m => m.UserId != userId)
            .Select(async m =>
            {
                var payload = JsonSerializer.Serialize(new { RoomId = requestRoomId, MessageId = messageId });
                var notification = Notification.Create(m.UserId, NotificationType.NewMessage, payload);
                await notificationRepository.AddAsync(notification);
                await backplane.SendToUserAsync(m.UserId, new
                {
                    type = "new_message",
                    notificationId = notification.Id,
                    requestRoomId,
                    userId,
                    content
                });
            });
        await Task.WhenAll(tasks);
    }
}