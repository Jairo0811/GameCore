using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using GameCore.Tests.Infrastructure;
using GameCore.WebAPI;

namespace GameCore.Tests;

public sealed class AuthTests : ApiTestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSignedTokenWithRole()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "  ADMIN@gamecore.local ", password = GameCoreApiFactory.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(session);
        Assert.Equal("Admin", session.Role);
        Assert.Equal(GameCoreApiFactory.AdminEmail, session.Email);
        Assert.True(session.ExpiresAt > DateTime.UtcNow.AddHours(7));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(session.Token);
        Assert.Equal("GameCore", jwt.Issuer);
        Assert.Contains(jwt.Claims, claim => claim.Value == "Admin");
    }

    [Theory]
    [InlineData(GameCoreApiFactory.AdminEmail, "wrong-password")]
    [InlineData("nobody@gamecore.local", GameCoreApiFactory.Password)]
    [InlineData("disabled@gamecore.local", GameCoreApiFactory.Password)]
    public async Task Login_WithInvalidCredentials_Returns401(string email, string password)
    {
        var response = await Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingFields_Returns400InsteadOf500()
    {
        var response = await Factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = (string?)null, password = "" });

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains("email", Errors(problem));
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await Factory.CreateClient().GetAsync("/api/games");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedToken_Returns401()
    {
        var client = await AdminAsync();
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token[..^4] + "AAAA");

        var response = await client.GetAsync("/api/games");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MalformedJsonBody_Returns400ProblemWithoutStackTrace()
    {
        var client = await AdminAsync();

        var response = await client.PostAsync("/api/customers", new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("   at ", body);
        Assert.DoesNotContain("Exception", body);
    }
}
