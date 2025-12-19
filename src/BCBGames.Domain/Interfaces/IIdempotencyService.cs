namespace BCBGames.Domain.Interfaces;

public interface IIdempotencyService
{
    string GenerateIdempotencyKey(
        Guid accountId,
        string transactionType,
        decimal amount,
        string? providedKey = null);

    Task<bool> IsDuplicateAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<bool> StoreIdempotencyKeyAsync(
        string idempotencyKey,
        int ttlHours = 24,
        CancellationToken cancellationToken = default);
}
