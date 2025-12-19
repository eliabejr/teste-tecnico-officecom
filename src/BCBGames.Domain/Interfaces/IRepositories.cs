using BCBGames.Domain.Entities;

namespace BCBGames.Domain.Interfaces;

/// <summary>
/// Repository para operações de persistência de contas (Accounts).
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Busca uma conta por ID. Usa cache quando disponível.
    /// </summary>
    /// <param name="id">Identificador único da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta encontrada ou null se não existir.</returns>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca uma conta por ID com tracking para atualização.
    /// Invalida o cache e retorna entidade trackeada pelo EF Core.
    /// O lock distribuído deve ser gerenciado pelo OrchestrateAsync quando necessário.
    /// </summary>
    /// <param name="id">Identificador único da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta encontrada ou null se não existir.</returns>
    Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova conta ao repositório.
    /// </summary>
    /// <param name="account">A conta a ser adicionada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A conta adicionada.</returns>
    Task<Account> AddAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma conta existente no repositório.
    /// Invalida o cache da conta após a atualização.
    /// </summary>
    /// <param name="account">A conta a ser atualizada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se uma conta existe pelo ID.
    /// </summary>
    /// <param name="id">Identificador único da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se a conta existe, false caso contrário.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository para operações de persistência de transações (Transactions).
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Busca uma transação por ID.
    /// </summary>
    /// <param name="id">Identificador único da transação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A transação encontrada ou null se não existir.</returns>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova transação ao repositório.
    /// </summary>
    /// <param name="transaction">A transação a ser adicionada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A transação adicionada.</returns>
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca transações de uma conta específica com paginação.
    /// </summary>
    /// <param name="accountId">Identificador da conta.</param>
    /// <param name="page">Número da página (baseado em 1).</param>
    /// <param name="pageSize">Tamanho da página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de transações da conta.</returns>
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(
        Guid accountId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta o total de transações de uma conta.
    /// </summary>
    /// <param name="accountId">Identificador da conta.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Total de transações da conta.</returns>
    Task<int> CountByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work pattern para gerenciar transações de banco de dados e repositórios.
/// Garante atomicidade e consistência em operações que envolvem múltiplas entidades.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Orquestra uma operação dentro de uma transação com retry automático em caso de conflitos de concorrência.
    /// Utiliza Execution Strategy do EF Core para lidar com falhas transitórias.
    /// </summary>
    /// <typeparam name="TResult">Tipo do resultado da operação.</typeparam>
    /// <param name="operation">A operação a ser executada dentro da transação.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>O resultado da operação.</returns>
    Task<TResult> OrchestrateAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Orquestra uma operação dentro de uma transação com lock distribuído e retry automático.
    /// Adquire e mantém o lock durante toda a transação, garantindo serialização de operações concorrentes.
    /// </summary>
    /// <typeparam name="TResult">Tipo do resultado da operação.</typeparam>
    /// <param name="lockKey">Chave para o lock distribuído (ex: "account-lock:{accountId}").</param>
    /// <param name="operation">A operação a ser executada dentro da transação.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>O resultado da operação.</returns>
    Task<TResult> OrchestrateAsync<TResult>(
        string lockKey,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Repository para operações de contas.
    /// </summary>
    IAccountRepository Accounts { get; }

    /// <summary>
    /// Repository para operações de transações.
    /// </summary>
    ITransactionRepository Transactions { get; }

    /// <summary>
    /// Salva todas as alterações pendentes no contexto do banco de dados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de entidades afetadas.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia uma nova transação de banco de dados.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma a transação atual, persistindo todas as alterações.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverte a transação atual, descartando todas as alterações não confirmadas.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
