namespace Application.Common.Interfaces.Services;

public interface INotificationService
{
    Task NotifyNewMessageAsync(Guid senderId, Guid roomId, Guid messageId, string content);
    Task NotifyPokeAsync(Guid pokerId, Guid targetUserId);
}