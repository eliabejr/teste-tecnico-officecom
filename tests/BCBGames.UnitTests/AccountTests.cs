using BCBGames.Domain.Entities;

namespace BCBGames.UnitTests;

public class AccountTests
{
    [Fact]
    public void Create_WhenOwnerNameIsNullOrWhitespace_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Account.Create(Guid.NewGuid(), "  ", 0));
    }

    [Fact]
    public void Create_WhenInitialBalanceIsNegative_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Account.Create(Guid.NewGuid(), "Eli", -0.01m));
    }

    [Fact]
    public void Create_ShouldTrimOwnerName_AndInitializeVersionAndBalance()
    {
        var account = Account.Create(Guid.NewGuid(), "Eliabe Serafim", 10m);

        Assert.Equal("Eliabe Serafim", account.OwnerName);
        Assert.Equal(10m, account.Balance);
        Assert.Equal(1, account.Version);
        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.False(string.IsNullOrWhiteSpace(account.AccountNumber));
    }

    [Fact]
    public void Credit_WhenAmountIsZeroOrNegative_ShouldThrow()
    {
        var account = Account.Create(Guid.NewGuid(), "Eli", 10m);

        Assert.Throws<ArgumentException>(() => account.Credit(0m));
        Assert.Throws<ArgumentException>(() => account.Credit(-1m));
    }

    [Fact]
    public void Credit_ShouldIncreaseBalance_AndIncrementVersion()
    {
        var account = Account.Create(Guid.NewGuid(), "Eli", 10m);

        account.Credit(2.5m);

        Assert.Equal(12.5m, account.Balance);
        Assert.Equal(2, account.Version);
    }

    [Fact]
    public void Debit_WhenAmountIsZeroOrNegative_ShouldThrow()
    {
        var account = Account.Create(Guid.NewGuid(), "Eli", 10m);

        Assert.Throws<ArgumentException>(() => account.Debit(0m));
        Assert.Throws<ArgumentException>(() => account.Debit(-1m));
    }

    [Fact]
    public void Debit_WhenInsufficientBalance_ShouldThrow()
    {
        var account = Account.Create(Guid.NewGuid(), "Eli", 1m);

        Assert.Throws<InvalidOperationException>(() => account.Debit(1.01m));
    }

    [Fact]
    public void Debit_ShouldDecreaseBalance_AndIncrementVersion()
    {
        var account = Account.Create(Guid.NewGuid(), "Eli", 10m);

        account.Debit(3m);

        Assert.Equal(7m, account.Balance);
        Assert.Equal(2, account.Version);
    }

    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(10, 9.99, true)]
    [InlineData(10, 10.01, false)]
    public void HasSufficientBalance_ShouldMatchExpected(decimal balance, decimal amount, bool expected)
    {
        var account = Account.Create(Guid.NewGuid(), "Eli", balance);

        Assert.Equal(expected, account.HasSufficientBalance(amount));
    }
}
