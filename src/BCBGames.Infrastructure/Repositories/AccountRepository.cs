using BCBGames.Domain.Entities;
using BCBGames.Domain.Interfaces;
using BCBGames.Infrastructure.Cache;
using BCBGames.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BCBGames.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AccountRepository> _logger;
    private const string CacheKeyPrefix = "account:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public AccountRepository(
        AppDbContext context,
        ICacheService cacheService,
        ILogger<AccountRepository> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        var cachedDto = await _cacheService.GetAsync<AccountCacheDto>(cacheKey, cancellationToken);
        if (cachedDto != null)
        {
            _logger.LogDebug("Account {AccountId} retrieved from cache", id);
            return MapFromDto(cachedDto);
        }

        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account != null)
        {
            var dto = MapToDto(account);
            await _cacheService.SetAsync(cacheKey, dto, CacheExpiration, cancellationToken);
            _logger.LogDebug("Account {AccountId} stored in cache", id);
        }

        return account;
    }

    public async Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account != null)
        {
            _logger.LogDebug("Account {AccountId} retrieved from database for update", id);
        }

        return account;
    }

    public async Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(account, cancellationToken);
        return account;
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);

        var cacheKey = $"{CacheKeyPrefix}{account.Id}";
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogDebug("Cache invalidated for account {AccountId}", account.Id);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";
        var cachedDto = await _cacheService.GetAsync<AccountCacheDto>(cacheKey, cancellationToken);
        if (cachedDto != null)
        {
            return true;
        }

        return await _context.Accounts.AnyAsync(a => a.Id == id, cancellationToken);
    }

    private static AccountCacheDto MapToDto(Account account)
    {
        return new AccountCacheDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            OwnerName = account.OwnerName,
            Balance = account.Balance,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            Version = account.Version
        };
    }

    private static readonly Dictionary<string, PropertyInfo> AccountProperties = new();

    private static Account MapFromDto(AccountCacheDto dto)
    {
        var account = (Account)Activator.CreateInstance(typeof(Account), nonPublic: true)!;

        if (AccountProperties.Count == 0)
        {
            var accountType = typeof(Account);
            AccountProperties[nameof(Account.Id)] = accountType.GetProperty(nameof(Account.Id), BindingFlags.Public | BindingFlags.Instance)!;
            AccountProperties[nameof(Account.AccountNumber)] = accountType.GetProperty(nameof(Account.AccountNumber), BindingFlags.Public | BindingFlags.Instance)!;
            AccountProperties[nameof(Account.OwnerName)] = accountType.GetProperty(nameof(Account.OwnerName), BindingFlags.Public | BindingFlags.Instance)!;
            AccountProperties[nameof(Account.Balance)] = accountType.GetProperty(nameof(Account.Balance), BindingFlags.Public | BindingFlags.Instance)!;
            AccountProperties[nameof(Account.CreatedAt)] = accountType.GetProperty(nameof(Account.CreatedAt), BindingFlags.Public | BindingFlags.Instance)!;
            AccountProperties[nameof(Account.UpdatedAt)] = accountType.GetProperty(nameof(Account.UpdatedAt), BindingFlags.Public | BindingFlags.Instance)!;
            AccountProperties[nameof(Account.Version)] = accountType.GetProperty(nameof(Account.Version), BindingFlags.Public | BindingFlags.Instance)!;
        }

        AccountProperties[nameof(Account.Id)].SetValue(account, dto.Id);
        AccountProperties[nameof(Account.AccountNumber)].SetValue(account, dto.AccountNumber);
        AccountProperties[nameof(Account.OwnerName)].SetValue(account, dto.OwnerName);
        AccountProperties[nameof(Account.Balance)].SetValue(account, dto.Balance);
        AccountProperties[nameof(Account.CreatedAt)].SetValue(account, dto.CreatedAt);
        AccountProperties[nameof(Account.UpdatedAt)].SetValue(account, dto.UpdatedAt);
        AccountProperties[nameof(Account.Version)].SetValue(account, dto.Version);

        return account;
    }
}
