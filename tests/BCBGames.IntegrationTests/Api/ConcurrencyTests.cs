using System.Net;
using System.Net.Http.Json;
using BCBGames.Application.DTOs;
using BCBGames.IntegrationTests.Infrastructure;

namespace BCBGames.IntegrationTests.Api;

public class ConcurrencyTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ConcurrencyTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Purchase_Concurrency_100SimultaneousRequests_ShouldNotLoseUpdates()
    {
        await _fixture.ClearDatabaseAsync();

        using var client = _fixture.CreateClient();

        var createAccount = await client.PostAsJsonAsync("/api/accounts", new
        {
            ownerName = "Concurrency Test",
            initialBalance = 100m
        });
        Assert.Equal(HttpStatusCode.Created, createAccount.StatusCode);
        var account = await createAccount.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(account);

        var tasks = Enumerable.Range(0, 100)
            .Select(i => client.PostAsJsonAsync("/api/transactions/purchase", new
            {
                accountId = account.Id,
                amount = 1m,
                merchant = $"M{i}"
            }))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        var nonCreated = responses.Where(r => r.StatusCode != HttpStatusCode.Created).ToList();
        if (nonCreated.Count != 0)
        {
            var details = await Task.WhenAll(nonCreated.Select(async r =>
            {
                var body = await r.Content.ReadAsStringAsync();
                return $"{(int)r.StatusCode} {r.StatusCode}: {body}";
            }));

            throw new Xunit.Sdk.XunitException(
                "Some purchases were not created:\n" + string.Join("\n", details));
        }

        var balanceResponse = await client.GetAsync($"/api/accounts/{account.Id}/balance");
        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);
        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.NotNull(balance);
        Assert.Equal(0m, balance.Balance);

        var statementResponse = await client.GetAsync($"/api/accounts/{account.Id}/statement?page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, statementResponse.StatusCode);
        var statement = await statementResponse.Content.ReadFromJsonAsync<StatementResponse>();
        Assert.NotNull(statement);
        Assert.Equal(100, statement.TotalTransactions);
        Assert.Equal(100, statement.Transactions.Count());
    }
}
