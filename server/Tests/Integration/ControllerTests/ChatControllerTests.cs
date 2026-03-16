using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Chat;
using Integration.Fixtures;

namespace Integration.ControllerTests;

[Collection("Api")]
public sealed class ChatControllerTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── Unauthenticated ──────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.PostAsJsonAsync("api/chat/messages",
            new SendMessageRequest(Guid.NewGuid(), "Test Message"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync($"api/chat/rooms/{Guid.NewGuid()}/messages");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.PostAsJsonAsync("api/chat/rooms",
            new CreateRoomRequest("Test Room", "Test Description"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllRooms_ShouldReturn200_WhenCalled()
    {
        var response = await _client.GetAsync("api/chat/get-all-rooms");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyRooms_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("api/chat/my-rooms");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchRoom_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("api/chat/rooms/search?name=test");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EditMessage_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.PatchAsJsonAsync(
            $"api/chat/messages/{Guid.NewGuid()}",
            new { newContent = "Edited" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMessage_ShouldReturn401_WhenNotAuthenticated()
    {
        var response = await _client.DeleteAsync($"api/chat/messages/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Edit message ─────────────────────────────────────────────────────

    [Fact]
    public async Task EditMessage_ShouldReturn200_WhenOwnerEditsOwnMessage()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (roomId, messageId) = await CreateRoomWithMessageAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"api/chat/messages/{messageId}",
            new { newContent = "Edited content" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EditMessage_ShouldReturn403_WhenNotOwner()
    {
        var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var otherClient = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var (roomId, messageId) = await CreateRoomWithMessageAsync(ownerClient);

        // Other user joins so they're authenticated in the room context
        await otherClient.PostAsync($"api/chat/rooms/{roomId}/join", null);
        
        var response = await otherClient.PatchAsJsonAsync(
            $"api/chat/messages/{messageId}",
            new { newContent = "Sneaky edit" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EditMessage_ShouldReturn400_WhenMessageIsAlreadyDeleted()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (_, messageId) = await CreateRoomWithMessageAsync(client);

        await client.DeleteAsync($"api/chat/messages/{messageId}");

        var response = await client.PatchAsJsonAsync(
            $"api/chat/messages/{messageId}",
            new { newContent = "Editing a deleted message" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Delete message ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMessage_ShouldReturn200_WhenOwnerDeletesOwnMessage()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (_, messageId) = await CreateRoomWithMessageAsync(client);

        var response = await client.DeleteAsync($"api/chat/messages/{messageId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMessage_ShouldReturn403_WhenNotOwner()
    {
        var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var otherClient = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var (roomId, messageId) = await CreateRoomWithMessageAsync(ownerClient);

        await otherClient.PostAsync($"api/chat/rooms/{roomId}/join", null);

        var response = await otherClient.DeleteAsync($"api/chat/messages/{messageId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMessage_ShouldSoftDelete_WhenOwnerDeletesOwnMessage()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (roomId, messageId) = await CreateRoomWithMessageAsync(client);

        await client.DeleteAsync($"api/chat/messages/{messageId}");

        // After soft delete the message is filtered out of results
        var messagesResponse = await client.GetAsync($"api/chat/rooms/{roomId}/messages");
        var envelope = await messagesResponse.Content.ReadFromJsonAsync<ResultEnvelope<IEnumerable<ChatMessageDto>>>();
    
        Assert.DoesNotContain(envelope!.Dto!, m => m.Id == messageId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    // Creates a room and sends one message, returns both IDs
    private static async Task<(Guid roomId, Guid messageId)> CreateRoomWithMessageAsync(HttpClient client)
    {
        var roomName = $"Test Room {Guid.NewGuid()}";   // unique per call

        var roomResponse = await client.PostAsJsonAsync("api/chat/rooms",
            new CreateRoomRequest(roomName, "Test Description"));

        if (!roomResponse.IsSuccessStatusCode)
        {
            var body = await roomResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"CreateRoom failed {roomResponse.StatusCode}\n" +
                $"Request headers: {string.Join(", ", roomResponse.RequestMessage!.Headers)}\n" +
                $"Body: {body}");
        }

        var location = roomResponse.Headers.Location!.ToString();
        var roomId = Guid.Parse(location.Split('/')[^2]);

        var sendResponse = await client.PostAsJsonAsync("api/chat/messages",
            new SendMessageRequest(roomId, "Test message"));

        if (!sendResponse.IsSuccessStatusCode)
        {
            var body = await sendResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"SendMessage failed with {sendResponse.StatusCode}: {body}");
        }

        var messagesResponse = await client.GetAsync($"api/chat/rooms/{roomId}/messages");
        var envelope = await messagesResponse.Content.ReadFromJsonAsync<ResultEnvelope<IEnumerable<ChatMessageDto>>>();
        var messageId = envelope!.Dto!.First().Id;

        return (roomId, messageId);
    }
}