namespace Application.Common.Interfaces.Services;

public interface IChatNotificationService
{
    Task NotifyRoomMemberAsync(Guid userId, Guid requestRoomId, Guid messageId);
}