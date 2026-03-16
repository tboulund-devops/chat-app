using System.Net;
using System.Net.Http.Json;
using Application.DTOs.Chat;
using Integration.Fixtures;

namespace Integration.ControllerTests;

[Collection("Api")]
public sealed class ChatControllerTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

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
}