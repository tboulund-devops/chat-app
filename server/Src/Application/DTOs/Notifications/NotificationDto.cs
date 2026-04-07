using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs.Notifications;

public class NotificationDto
{
    public Guid Id { get; init; }
    public NotificationType Type { get; init; }
    public string Payload { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }

    public static NotificationDto Map(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Payload = n.Payload,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}