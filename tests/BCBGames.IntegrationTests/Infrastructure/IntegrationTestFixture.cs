using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BCBGames.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BCBGames.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    public CustomWebApplicationFactory Factory { get; private set; } = null!;

    public HttpClient CreateClient()
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        client.Timeout = TimeSpan.FromMinutes(5);
        return client;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string? email = null,
        string password = "Password123!")
    {
        var client = CreateClient();

        email ??= $"test-{Guid.NewGuid():N}@local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        if (register.IsSuccessStatusCode is false)
        {
            // If already exists (or any other error), fallback to login.
            var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
            login.EnsureSuccessStatusCode();
            var logged = await login.Content.ReadFromJsonAsync<AuthResponse>();
            if (logged is null) throw new InvalidOperationException("Could not deserialize login response.");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", logged.AccessToken);
            return client;
        }

        var created = await register.Content.ReadFromJsonAsync<AuthResponse>();
        if (created is null) throw new InvalidOperationException("Could not deserialize register response.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", created.AccessToken);
        return client;
    }

    private sealed record AuthResponse(Guid UserId, string Email, string Role, string AccessToken, DateTime ExpiresAtUtc);

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("bcb_games_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgres.StartAsync();

        Factory = new CustomWebApplicationFactory(_postgres.GetConnectionString());

        using var client = CreateClient();
        _ = await client.GetAsync("/health");
    }

    public async Task ClearDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Transactions\" RESTART IDENTITY CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Accounts\" RESTART IDENTITY CASCADE");
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Users\" RESTART IDENTITY CASCADE");
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            Factory.Dispose();

        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }
}
