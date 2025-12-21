using System.Net;
using System.Net.Http.Json;
using BCBGames.Application.DTOs;
using BCBGames.IntegrationTests.Infrastructure;

namespace BCBGames.IntegrationTests.Api;

public class TransactionsEndpointsTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public TransactionsEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task DepositWithdrawPurchase_ShouldPersistAndAppearInStatement()
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var createAccount = await client.PostAsJsonAsync("/api/accounts", new { ownerName = "Tx Test", initialBalance = 10m });
        Assert.Equal(HttpStatusCode.Created, createAccount.StatusCode);
        var account = await createAccount.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(account);

        var deposit = await client.PostAsJsonAsync("/api/transactions/deposit", new
        {
            accountId = account.Id,
            amount = 5m,
            description = "pix"
        });
        Assert.Equal(HttpStatusCode.Created, deposit.StatusCode);

        var withdraw = await client.PostAsJsonAsync("/api/transactions/withdraw", new
        {
            accountId = account.Id,
            amount = 3m,
            description = "saque"
        });
        Assert.Equal(HttpStatusCode.Created, withdraw.StatusCode);

        var purchase = await client.PostAsJsonAsync("/api/transactions/purchase", new
        {
            accountId = account.Id,
            amount = 2m,
            merchant = "BCB Games"
        });
        Assert.Equal(HttpStatusCode.Created, purchase.StatusCode);

        var balanceResponse = await client.GetAsync($"/api/accounts/{account.Id}/balance");
        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);
        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.NotNull(balance);
        Assert.Equal(10m, balance.Balance);

        var statementResponse = await client.GetAsync($"/api/accounts/{account.Id}/statement?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, statementResponse.StatusCode);
        var statement = await statementResponse.Content.ReadFromJsonAsync<StatementResponse>();
        Assert.NotNull(statement);
        Assert.Equal(account.Id, statement.AccountId);
        Assert.True(statement.TotalTransactions >= 3);
    }
}
