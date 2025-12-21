using System.Net;
using System.Net.Http.Json;
using BCBGames.IntegrationTests.Infrastructure;

namespace BCBGames.IntegrationTests.Api;

public class ValidationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ValidationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAccount_WhenOwnerNameTooShort_ShouldReturnBadRequest()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var resp = await client.PostAsJsonAsync("/api/accounts", new { ownerName = "A", initialBalance = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Purchase_WhenAmountBelowMinimum_ShouldReturnBadRequest()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/accounts", new { ownerName = "Val Test", initialBalance = 10m });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var account = await create.Content.ReadFromJsonAsync<BCBGames.Application.DTOs.AccountResponse>();
        Assert.NotNull(account);

        var resp = await client.PostAsJsonAsync("/api/transactions/purchase", new { accountId = account.Id, amount = 0.001m, merchant = "BCB Games" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Purchase_WhenMerchantMissing_ShouldReturnBadRequest()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/accounts", new { ownerName = "Val Test 2", initialBalance = 10m });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var account = await create.Content.ReadFromJsonAsync<BCBGames.Application.DTOs.AccountResponse>();
        Assert.NotNull(account);

        var resp = await client.PostAsJsonAsync("/api/transactions/purchase", new { accountId = account.Id, amount = 1m, merchant = "" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
