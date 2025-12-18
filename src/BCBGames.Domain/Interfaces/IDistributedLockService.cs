namespace BCBGames.Domain.Interfaces;

/// <summary>
/// Serviço para gerenciar locks distribuídos, garantindo exclusividade mútua entre múltiplas instâncias da aplicação.
/// </summary>
public interface IDistributedLockService : IDisposable
{
    /// <summary>
    /// Adquire um lock distribuído para uma chave específica.
    /// </summary>
    /// <param name="key">Chave única do lock.</param>
    /// <param name="expiry">Tempo de expiração do lock. Padrão: 30 segundos.</param>
    /// <param name="waitTime">Tempo máximo para aguardar a aquisição do lock. Padrão: 10 segundos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Handle do lock adquirido ou null se não foi possível adquirir dentro do tempo de espera.</returns>
    Task<IDistributedLockHandle?> AcquireLockAsync(
        string key,
        TimeSpan? expiry = null,
        TimeSpan? waitTime = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Handle de um lock distribuído adquirido. Deve ser descartado (disposed) quando o lock não for mais necessário.
/// </summary>
public interface IDistributedLockHandle : IDisposable
{
    /// <summary>
    /// Chave do lock associado a este handle.
    /// </summary>
    string LockKey { get; }

    /// <summary>
    /// Indica se o lock ainda está adquirido e válido.
    /// </summary>
    bool IsAcquired { get; }

    /// <summary>
    /// Estende o tempo de expiração do lock.
    /// </summary>
    /// <param name="expiry">Novo tempo de expiração.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>True se o lock foi estendido com sucesso, false caso contrário.</returns>
    Task<bool> ExtendAsync(TimeSpan expiry, CancellationToken cancellationToken = default);
}
