using System.Text.Json;
using Infrastructure.Sse;

namespace Unit;

public class InMemorySimpleSseTests : IDisposable
{
    private readonly InMemorySimpleSse _sse = new();
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
            _sse.Dispose();
        _disposed = true;
    }

    [Fact]
    public async Task DisconnectAsync_ShouldReturnEarly_WhenConnectionNotFound()
    {
        var ex = await Record.ExceptionAsync(() => _sse.DisconnectAsync(Guid.NewGuid()));
        Assert.Null(ex);
    }

    [Fact]
    public async Task DisconnectAsync_ShouldCleanUpEmptyGroups()
    {
        var (connectionId, _) = _sse.Connect();
        var groupId = Guid.NewGuid();
        await _sse.AddToGroupAsync(connectionId, groupId);
        await _sse.DisconnectAsync(connectionId);

        var ex = await Record.ExceptionAsync(() => _sse.SendToGroupAsync(groupId, new { test = "data" }));
        Assert.Null(ex);
    }

    [Fact]
    public async Task SendToGroupAsync_ShouldDoNothing_WhenGroupNotFound()
    {
        var ex = await Record.ExceptionAsync(() => _sse.SendToGroupAsync(Guid.NewGuid(), new { text = "ghost" }));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        _sse.Dispose();
        var ex = Record.Exception(() => _sse.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void Constructor_ShouldUseProvidedJsonOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        using var sse = new InMemorySimpleSse(options);
        var (connectionId, channel) = sse.Connect();
        Assert.NotEqual(Guid.Empty, connectionId);
        Assert.NotNull(channel);
    }
    
    [Fact]
public void Connect_ShouldReturnConnectionIdAndChannel()
{
    var (connectionId, channel) = _sse.Connect();
    Assert.NotEqual(Guid.Empty, connectionId);
    Assert.NotNull(channel);
}

[Fact]
public async Task AddToGroupAsync_ShouldThrow_WhenConnectionNotFound()
{
    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _sse.AddToGroupAsync(Guid.NewGuid(), Guid.NewGuid()));
}

[Fact]
public async Task AddToGroupAsync_ShouldAllowSendingToGroup_AfterAdding()
{
    var (connectionId, channel) = _sse.Connect();
    var groupId = Guid.NewGuid();
    await _sse.AddToGroupAsync(connectionId, groupId);

    await _sse.SendToGroupAsync(groupId, new { msg = "hello" });

    Assert.True(channel.Reader.TryRead(out _));
}

[Fact]
public async Task SendToGroupAsync_ShouldDeliverMessageToAllMembers()
{
    var (connId1, channel1) = _sse.Connect();
    var (connId2, channel2) = _sse.Connect();
    var groupId = Guid.NewGuid();

    await _sse.AddToGroupAsync(connId1, groupId);
    await _sse.AddToGroupAsync(connId2, groupId);

    await _sse.SendToGroupAsync(groupId, new { text = "broadcast" });

    Assert.True(channel1.Reader.TryRead(out _));
    Assert.True(channel2.Reader.TryRead(out _));
}

[Fact]
public async Task SendToUserAsync_ShouldDeliverMessageToUser()
{
    var (connectionId, channel) = _sse.Connect();
    var userId = Guid.NewGuid();
    await _sse.SubscribeUserAsync(connectionId, userId);

    await _sse.SendToUserAsync(userId, new { alert = "ping" });

    Assert.True(channel.Reader.TryRead(out _));
}

[Fact]
public async Task SubscribeUserAsync_ShouldAddConnectionToUserGroup()
{
    var (connectionId, channel) = _sse.Connect();
    var userId = Guid.NewGuid();
    await _sse.SubscribeUserAsync(connectionId, userId);

    await _sse.SendToGroupAsync(userId, new { msg = "user msg" });

    Assert.True(channel.Reader.TryRead(out _));
}

[Fact]
public async Task DisconnectAsync_ShouldRemoveConnection()
{
    var (connectionId, _) = _sse.Connect();
    await _sse.DisconnectAsync(connectionId);

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
        _sse.AddToGroupAsync(connectionId, Guid.NewGuid()));
}

[Fact]
public void Dispose_ShouldCompleteAllChannels()
{
    var (_, channel1) = _sse.Connect();
    var (_, channel2) = _sse.Connect();

    _sse.Dispose();

    Assert.True(channel1.Reader.Completion.IsCompleted);
    Assert.True(channel2.Reader.Completion.IsCompleted);
}
}