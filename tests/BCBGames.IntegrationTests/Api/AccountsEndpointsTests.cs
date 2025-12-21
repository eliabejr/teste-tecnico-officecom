using System.Net;
using System.Net.Http.Json;
using BCBGames.Application.DTOs;
using BCBGames.IntegrationTests.Infrastructure;

namespace BCBGames.IntegrationTests.Api;

public class AccountsEndpointsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public AccountsEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAccount_ThenGetAccountAndBalance_ShouldReturnExpected()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/accounts", new
        {
            ownerName = "Integration Test",
            initialBalance = 123.45m
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(created);
        Assert.Equal("Integration Test", created.OwnerName);
        Assert.Equal(123.45m, created.Balance);

        var getResponse = await client.GetAsync($"/api/accounts/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);

        var balanceResponse = await client.GetAsync($"/api/accounts/{created.Id}/balance");
        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);

        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.NotNull(balance);
        Assert.Equal(created.Id, balance.AccountId);
        Assert.Equal(123.45m, balance.Balance);
    }
}
