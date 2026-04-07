using Domain.Entities;

public class RoomMemberDto
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    public static RoomMemberDto Map(ChatRoomMember m) => new()
    {
        UserId = m.UserId,
        FirstName = m.User.FirstName,
        LastName = m.User.LastName,
        Email = m.User.Email,
    };
}