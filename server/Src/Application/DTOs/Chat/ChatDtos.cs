using System.Runtime.CompilerServices;
using Application.DTOs.Entities;
using Domain.Entities;

namespace Application.DTOs.Chat;

public record ChatMessageDto(
    Guid Id,
    Guid RoomId,
    UserDto? Sender,
    string Content,
    DateTime CreatedAt
);

public record ChatRoomDto()
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public UserDto? Owner { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public IEnumerable<UserDto>? Members { get; init; }

    public static ChatRoomDto Map(ChatRoom chatRoom)
    {
        return new ChatRoomDto
        {
            Id = chatRoom.Id,
            Name = chatRoom.Name,
            Description = chatRoom.Description,
            CreatedAt = chatRoom.CreatedAt,
            Members = chatRoom.Members.Select(member => UserDto.Map(member.User)).OfType<UserDto>(),
            Owner = UserDto.Map(chatRoom.CreatedBy)
        };
    }
}



public record SendMessageRequest(
    Guid RoomId,
    string Content
);

public record CreateRoomRequest(
    string Name,
    string? Description
);

public record JoinRoomRequest(
    Guid RoomId
);
