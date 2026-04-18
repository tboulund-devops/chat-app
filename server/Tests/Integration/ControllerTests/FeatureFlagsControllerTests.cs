using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Integration.Fixtures;

namespace Integration.ControllerTests;


[Collection("Api")]
public sealed class FeatureFlagsControllerTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetRegisterUserFlag_ShouldReturn200_WhenCalled()
    {
        var response = await _client.GetAsync("api/features/register-user", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRegisterUserFlag_ShouldReturnEnabledField_InBody()
    {
        var response = await _client.GetAsync("api/features/register-user", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(body.TryGetProperty("enabled", out _));
    }

    [Fact]
    public async Task GetRegisterUserFlag_ShouldReturnTrue_WhenAlwaysEnabledProviderIsUsed()
    {
        var response = await _client.GetAsync("api/features/register-user", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(body.GetProperty("enabled").GetBoolean());
    }
    
    [Fact]
    public async Task Debug_ShouldShowResponseBody()
    {
        var response = await _client.GetAsync("api/features/register-user", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Console.WriteLine($"Status: {response.StatusCode}, Body: {body}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}