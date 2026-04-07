using System.Net;
using System.Net.Http.Json;
using Integration.Fixtures;

namespace Integration.ControllerTests;

[Collection("Api")]
public sealed class NotificationControllerTests(ApiFactory factory)
{
    // ── Unauthenticated ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUnread_ShouldReturn401_WhenNotAuthenticated()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("api/notifications", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Poke_ShouldReturn401_WhenNotAuthenticated()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsync($"api/notifications/poke/{Guid.NewGuid()}", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_ShouldReturn401_WhenNotAuthenticated()
    {
        var client = factory.CreateClient();
        var response = await client.PatchAsync($"api/notifications/{Guid.NewGuid()}/read", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_ShouldReturn401_WhenNotAuthenticated()
    {
        var client = factory.CreateClient();
        var response = await client.PatchAsync("api/notifications/read-all", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GetUnread ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUnread_ShouldReturn200WithEmptyList_WhenNoNotifications()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var response = await client.GetAsync("api/notifications", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<IEnumerable<object>>(TestContext.Current.CancellationToken);
        Assert.NotNull(notifications);
    }

    // ── Poke ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Poke_ShouldReturn404_WhenTargetUserDoesNotExist()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var response = await client.PostAsync($"api/notifications/poke/{Guid.NewGuid()}", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Poke_ShouldReturn200_WhenTargetUserExists()
    {
        var (pokerClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (targetClient, targetUserId) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await pokerClient.PostAsync(
            $"api/notifications/poke/{targetUserId}",
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Poke_ShouldCreateNotification_ForTargetUser()
    {
        var (pokerClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (targetClient, targetUserId) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        await pokerClient.PostAsync($"api/notifications/poke/{targetUserId}", null, TestContext.Current.CancellationToken);

        var notificationsResponse = await targetClient.GetAsync("api/notifications", TestContext.Current.CancellationToken);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<IEnumerable<NotificationDto>>(TestContext.Current.CancellationToken);

        Assert.NotNull(notifications);
        Assert.Contains(notifications, n => n.Type == 0);
    }

    // ── MarkRead ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkRead_ShouldReturn404_WhenNotificationDoesNotExist()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var response = await client.PatchAsync($"api/notifications/{Guid.NewGuid()}/read", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_ShouldReturn403_WhenNotificationBelongsToAnotherUser()
    {
        var (pokerClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (targetClient, targetUserId) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (otherClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        await pokerClient.PostAsync($"api/notifications/poke/{targetUserId}", null, TestContext.Current.CancellationToken);

        var notificationsResponse = await targetClient.GetAsync("api/notifications", TestContext.Current.CancellationToken);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<IEnumerable<NotificationDto>>(TestContext.Current.CancellationToken);
        var notification = notifications?.FirstOrDefault();
        Assert.NotNull(notification);

        var response = await otherClient.PatchAsync($"api/notifications/{notification.Id}/read", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_ShouldReturn204_WhenOwnerMarksAsRead()
    {
        var (pokerClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (targetClient, targetUserId) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        await pokerClient.PostAsync($"api/notifications/poke/{targetUserId}", null, TestContext.Current.CancellationToken);

        var notificationsResponse = await targetClient.GetAsync("api/notifications", TestContext.Current.CancellationToken);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<IEnumerable<NotificationDto>>(TestContext.Current.CancellationToken);
        var notification = notifications?.FirstOrDefault();
        Assert.NotNull(notification);

        var response = await targetClient.PatchAsync($"api/notifications/{notification.Id}/read", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_ShouldRemoveFromUnread_AfterMarking()
    {
        var (pokerClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (targetClient, targetUserId) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        await pokerClient.PostAsync($"api/notifications/poke/{targetUserId}", null, TestContext.Current.CancellationToken);

        var notificationsResponse = await targetClient.GetAsync("api/notifications", TestContext.Current.CancellationToken);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<IEnumerable<NotificationDto>>(TestContext.Current.CancellationToken);
        var notification = notifications?.FirstOrDefault();
        Assert.NotNull(notification);

        await targetClient.PatchAsync($"api/notifications/{notification.Id}/read", null, TestContext.Current.CancellationToken);

        var afterResponse = await targetClient.GetAsync("api/notifications", TestContext.Current.CancellationToken);
        var afterNotifications = await afterResponse.Content.ReadFromJsonAsync<IEnumerable<NotificationDto>>(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(afterNotifications!, n => n.Id == notification.Id);
    }

    // ── MarkAllRead ──────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllRead_ShouldReturn204_WhenCalled()
    {
        var (client, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var response = await client.PatchAsync("api/notifications/read-all", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_ShouldClearAllUnread_WhenUserHasNotifications()
    {
        var (pokerClient, _) = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var (targetClient, targetUserId) = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        await pokerClient.PostAsync($"api/notifications/poke/{targetUserId}", null, TestContext.Current.CancellationToken);
        await pokerClient.PostAsync($"api/notifications/poke/{targetUserId}", null, TestContext.Current.CancellationToken);

        await targetClient.PatchAsync("api/notifications/read-all", null, TestContext.Current.CancellationToken);

        var afterResponse = await targetClient.GetAsync("api/notifications", TestContext.Current.CancellationToken);
        var afterNotifications = await afterResponse.Content.ReadFromJsonAsync<IEnumerable<NotificationDto>>(TestContext.Current.CancellationToken);
        Assert.Empty(afterNotifications!);
    }
}

file record NotificationDto(
    [property: System.Text.Json.Serialization.JsonPropertyName("id")] Guid Id,
    [property: System.Text.Json.Serialization.JsonPropertyName("type")] int Type,
    [property: System.Text.Json.Serialization.JsonPropertyName("payload")] string Payload,
    [property: System.Text.Json.Serialization.JsonPropertyName("isRead")] bool IsRead,
    [property: System.Text.Json.Serialization.JsonPropertyName("createdAt")] string CreatedAt
);