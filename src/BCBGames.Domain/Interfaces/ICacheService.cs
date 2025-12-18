namespace BCBGames.Domain.Interfaces;

/// <summary>
/// Serviço de cache distribuído para armazenar e recuperar dados.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Recupera um valor do cache pela chave.
    /// </summary>
    /// <typeparam name="T">Tipo do objeto a ser recuperado.</typeparam>
    /// <param name="key">Chave do item no cache.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O valor encontrado no cache ou null se não existir.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Armazena um valor no cache com opcional expiração.
    /// </summary>
    /// <typeparam name="T">Tipo do objeto a ser armazenado.</typeparam>
    /// <param name="key">Chave do item no cache.</param>
    /// <param name="value">Valor a ser armazenado.</param>
    /// <param name="expiration">Tempo de expiração do item. Se não especificado, usa o padrão (5 minutos).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove um item do cache pela chave.
    /// </summary>
    /// <param name="key">Chave do item a ser removido.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
