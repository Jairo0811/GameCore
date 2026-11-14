using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GameCore.Tests.Infrastructure;

/// <summary>A fresh API + database per test.</summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    protected GameCoreApiFactory Factory { get; } = new();

    public Task InitializeAsync() => Factory.SeedAsync();

    public async Task DisposeAsync() => await Factory.DisposeAsync();

    protected Task<HttpClient> AdminAsync() => Factory.CreateClientAsAsync(GameCoreApiFactory.AdminEmail);
    protected Task<HttpClient> ManagerAsync() => Factory.CreateClientAsAsync(GameCoreApiFactory.ManagerEmail);
    protected Task<HttpClient> ViewerAsync() => Factory.CreateClientAsAsync(GameCoreApiFactory.ViewerEmail);

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>Asserts an RFC 7807 problem response with the given status code.</summary>
    protected static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await ReadJsonAsync(response);
        Assert.Equal((int)expected, body.GetProperty("status").GetInt32());
        return body;
    }

    protected static string Errors(JsonElement problem) =>
        problem.TryGetProperty("errors", out var errors) ? errors.GetRawText() : problem.GetProperty("detail").GetString() ?? string.Empty;
}
