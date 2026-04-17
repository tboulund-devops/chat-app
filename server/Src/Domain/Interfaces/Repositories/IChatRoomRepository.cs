using Domain.Entities;

namespace Domain.Interfaces.Repositories;

public interface IChatRoomRepository : IBaseRepository<ChatRoom>
{
    Task<ChatRoom?> GetByIdWithMembersAsync(Guid roomId);
    Task<IEnumerable<ChatRoom>> GetRoomsForUserAsync(Guid userId);
    Task<bool> AddMemberAsync(Guid roomId, Guid userId);
    Task<bool> RemoveMemberAsync(Guid roomId, Guid userId);
    Task<bool> IsMemberAsync(Guid roomId, Guid userId);
    Task<IEnumerable<ChatRoom>> SearchRoomsByNameAsync(string name);
    Task<IEnumerable<ChatRoom>> GetAllRoomsAsync();
    Task<IEnumerable<ChatRoomMember>> GetMembersAsync(Guid roomId);
}
