using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace BCBGames.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;

    public CustomWebApplicationFactory Factory { get; private set; } = null!;

    public HttpClient CreateClient() => Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

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

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            Factory.Dispose();

        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }
}
