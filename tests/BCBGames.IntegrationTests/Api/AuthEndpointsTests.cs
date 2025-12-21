using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BCBGames.IntegrationTests.Infrastructure;

namespace BCBGames.IntegrationTests.Api;

public class AuthEndpointsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public AuthEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    private sealed record AuthResponse(Guid UserId, string Email, string Role, string AccessToken, DateTime ExpiresAtUtc);
    private sealed record MeResponse(Guid Id, string Email, string Role, DateTime CreatedAt);

    [Fact]
    public async Task Register_WhenValid_ShouldReturnCreated_AndTokenShouldWorkOnMe()
    {
        await _fixture.ClearDatabaseAsync();
        using var client = _fixture.CreateClient();

        var email = $"auth-{Guid.NewGuid():N}@bcbgames";
        var password = "Password123!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var created = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.UserId);
        Assert.Equal(email, created.Email);
        Assert.Equal("User", created.Role);
        Assert.False(string.IsNullOrWhiteSpace(created.AccessToken));
        Assert.True(created.ExpiresAtUtc > DateTime.UtcNow);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", created.AccessToken);
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var meBody = await me.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(meBody);
        Assert.Equal(created.UserId, meBody.Id);
        Assert.Equal(created.Email, meBody.Email);
        Assert.Equal(created.Role, meBody.Role);
    }

    [Fact]
    public async Task Register_WhenPasswordTooShort_ShouldReturnBadRequest()
    {
        await _fixture.ClearDatabaseAsync();
        using var client = _fixture.CreateClient();

        var email = $"auth-{Guid.NewGuid():N}@bcbgames";
        var password = "1234567";

        var resp = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyRegistered_ShouldReturnConflict()
    {
        await _fixture.ClearDatabaseAsync();
        using var client = _fixture.CreateClient();

        var email = $"auth-{Guid.NewGuid():N}@bcbgames";
        var password = "Password123!";

        var first = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ShouldReturnUnauthorized()
    {
        await _fixture.ClearDatabaseAsync();
        using var client = _fixture.CreateClient();

        var email = $"missing-{Guid.NewGuid():N}@bcbgames";
        var password = "Password123!";

        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ShouldReturnUnauthorized()
    {
        await _fixture.ClearDatabaseAsync();
        using var client = _fixture.CreateClient();

        var email = $"auth-{Guid.NewGuid():N}@bcbgames";
        var password = "Password123!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_WhenValid_ShouldReturnOk_AndTokenShouldWorkOnMe()
    {
        await _fixture.ClearDatabaseAsync();
        using var client = _fixture.CreateClient();

        var email = $"auth-{Guid.NewGuid():N}@bcbgames";
        var password = "Password123!";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var logged = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(logged);
        Assert.Equal(email, logged.Email);
        Assert.False(string.IsNullOrWhiteSpace(logged.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", logged.AccessToken);
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Me_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        using var client = _fixture.CreateClient();
        var resp = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}

