using Domain.Enums;

namespace Domain.Entities;

public sealed record Notification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid RecipientId { get; init; }
    public User Recipient { get; init; } = null!;

    public NotificationType Type { get; init; }   // Poke, NewMessage, etc.
    public string Payload { get; init; } = string.Empty; // JSON blob
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public static Notification Create(Guid recipientId, NotificationType type, string payload) =>
        new() { RecipientId = recipientId, Type = type, Payload = payload };
}