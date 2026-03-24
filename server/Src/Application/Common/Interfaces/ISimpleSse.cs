using System.Text.Json;
using System.Threading.Channels;

namespace Application.Common.Interfaces;

public readonly record struct SseEvent(Guid? Group, JsonElement Data);
public interface ISimpleSse
{
    (Guid ConnectionId, Channel<SseEvent> Reader) Connect();
    Task DisconnectAsync(Guid connectionId);
    Task AddToGroupAsync(Guid connectionId, Guid groupId);
    Task SendToGroupAsync(Guid groupId, object message);
    Task SendToUserAsync(Guid userId, object message);
    Task SubscribeUserAsync(Guid connectionId, Guid userId);
}