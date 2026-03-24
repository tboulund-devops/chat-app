using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using System.Threading.Channels;
using Application.Common.Interfaces;

namespace Infrastructure.Sse;

public class InMemorySimpleSse : ISimpleSse, IDisposable
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConcurrentDictionary<Guid, ConnectionState>  _connections = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, byte>> _groups = new();
    
    public InMemorySimpleSse(JsonSerializerOptions? jsonOptions = null)
    {
        _jsonSerializerOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }
    
    private sealed class ConnectionState(Channel<SseEvent>channel)
    {
        public Channel<SseEvent> Channel { get; } = channel;
        public ConcurrentDictionary<Guid, byte> Groups { get; } = new();

    }
    
    public (Guid ConnectionId, Channel<SseEvent> Reader) Connect()
    {
        var channel = Channel.CreateUnbounded<SseEvent>();
        var connectionId = Guid.NewGuid();
        var state = new  ConnectionState(channel);
        
        _connections.TryAdd(connectionId, state);
        
        return (connectionId, channel);
    }

    public Task DisconnectAsync(Guid connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var state))
        {
            return Task.CompletedTask;
        }
        
        // all groups that client belongs to
        var clientGroups = state.Groups.Keys.ToList();
        
        // for each of this groups try to find them in the _groups dictionary and remove itself
        foreach (var groupId in clientGroups)
        {
            if (_groups.TryGetValue(groupId, out var members))
            {
                members.TryRemove(connectionId, out _);
                if (members.IsEmpty)
                {
                    _groups.TryRemove(groupId, out _);
                }
                
            }
        }
        state.Channel.Writer.Complete();
        return Task.CompletedTask;
    }

    public Task AddToGroupAsync(Guid connectionId, Guid groupId)
    {
        if (!_connections.TryGetValue(connectionId, out var state))
            throw new Exception("connection is not connected");

        Console.WriteLine("Adding to group " + groupId);
        state.Groups.TryAdd(groupId, 0);

        var members = _groups.GetOrAdd(groupId,
            _ => new ConcurrentDictionary<Guid, byte>());

        members.TryAdd(connectionId, 0);

        Console.WriteLine("Group " + members.Count);
        return Task.CompletedTask;
    }

    public async Task SendToGroupAsync(Guid groupId, object message)
    {
        if (!_groups.TryGetValue(groupId, out var members))
        {
            return;
        }
        
        var json = JsonSerializer.SerializeToElement(message, _jsonSerializerOptions);
        var evt = new SseEvent(groupId, json);
        
        Console.WriteLine("Sending message to group " + groupId);
        
        var tasks = members.Keys
            .Select(id =>
            {
                if (_connections.TryGetValue(id, out var state))
                {
                    return state.Channel.Writer.WriteAsync(evt).AsTask();
                }

                return Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }

    public Task SendToUserAsync(Guid userId, object message)
    {
        return SendToGroupAsync(userId, message);
    }

    public Task SubscribeUserAsync(Guid connectionId, Guid userId)
    {
        return AddToGroupAsync(connectionId, userId);
    }


    public void Dispose()
    {
        foreach (var state in _connections.Values)
        {
            state.Channel.Writer.Complete();
        }
        _connections.Clear();
        _groups.Clear();
        
        
    }
    
}