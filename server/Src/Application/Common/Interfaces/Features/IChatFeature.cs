using Application.Common.Results;
using Application.DTOs.Chat;

namespace Application.Common.Interfaces.Features;

public interface IChatFeature
{
    Task<Result<ChatMessageDto>> CreateMessageAsync(Guid userId, SendMessageRequest request);
    Task<Result<IEnumerable<ChatMessageDto>>> GetMessagesAsync(Guid userId, Guid roomId, int skip = 0, int take = 50);
    Task<Result<ChatRoomDto>> CreateRoomAsync(Guid userId, CreateRoomRequest request);
    Task<Result<IEnumerable<ChatRoomDto>>> GetUserRoomsAsync(Guid userId);
    Task<Result> JoinRoomAsync(Guid userId, Guid roomId);
    Task<Result> LeaveRoomAsync(Guid userId, Guid roomId);
    Task<Result<IEnumerable<ChatRoomDto>>> SearchRoomByNameAsync(string name);
    Task<Result> EditMessageAsync(Guid userId, Guid messageId, string newContent);
    Task<Result> DeleteMessageAsync(Guid userId, Guid messageId);
}