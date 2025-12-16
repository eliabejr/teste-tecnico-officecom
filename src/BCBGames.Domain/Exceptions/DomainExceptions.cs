namespace BCBGames.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid accountId) 
        : base($"Account with ID '{accountId}' was not found") { }
}

public class TransactionNotFoundException : DomainException
{
    public TransactionNotFoundException(Guid transactionId) 
        : base($"Transaction with ID '{transactionId}' was not found") { }
}

public class InsufficientBalanceException : DomainException
{
    public InsufficientBalanceException(decimal balance, decimal amount) 
        : base($"Insufficient balance. Current: R$ {balance:N2}, Required: R$ {amount:N2}") { }
}

public class ConcurrencyException : DomainException
{
    public ConcurrencyException() 
        : base("Outdated data. Please retry.") { }
}

public class InvalidAmountException : DomainException
{
    public InvalidAmountException() 
        : base("Amount must be at least R$ 0.01") { }
}
