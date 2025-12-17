using BCBGames.Domain.Entities;
using BCBGames.Domain.Enums;

namespace BCBGames.UnitTests;

public class TransactionTests
{
    [Fact]
    public void Create_WhenAmountIsBelowMinimum_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Transaction.Create(Guid.NewGuid(), TransactionType.Deposit, 0.009m, 0m, "x"));

        Assert.Contains("Minimum amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ShouldInitializePendingAndBalances()
    {
        var accountId = Guid.NewGuid();
        var nowBefore = DateTime.UtcNow;

        var t = Transaction.Create(accountId, TransactionType.Purchase, 1.23m, 10m, "  Test  ");

        Assert.NotEqual(Guid.Empty, t.Id);
        Assert.Equal(accountId, t.AccountId);
        Assert.Equal(TransactionType.Purchase, t.Type);
        Assert.Equal(1.23m, t.Amount);
        Assert.Equal("Test", t.Description);
        Assert.Equal(10m, t.BalanceBefore);
        Assert.Equal(10m, t.BalanceAfter);
        Assert.Equal(TransactionStatus.Pending, t.Status);
        Assert.True(t.CreatedAt >= nowBefore);
        Assert.Null(t.ProcessedAt);
    }

    [Fact]
    public void Complete_ShouldSetCompletedStatus_ProcessedAt_AndBalanceAfter()
    {
        var t = Transaction.Create(Guid.NewGuid(), TransactionType.Withdraw, 1m, 10m, null);

        t.Complete(9m);

        Assert.Equal(9m, t.BalanceAfter);
        Assert.Equal(TransactionStatus.Completed, t.Status);
        Assert.NotNull(t.ProcessedAt);
    }

    [Fact]
    public void Fail_ShouldSetFailedStatus_AndProcessedAt()
    {
        var t = Transaction.Create(Guid.NewGuid(), TransactionType.Withdraw, 1m, 10m, null);

        t.Fail();

        Assert.Equal(TransactionStatus.Failed, t.Status);
        Assert.NotNull(t.ProcessedAt);
    }
}
