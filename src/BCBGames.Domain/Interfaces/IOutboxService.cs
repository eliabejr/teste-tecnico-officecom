using BCBGames.Domain.Events;

namespace BCBGames.Domain.Interfaces;

public interface IOutboxService
{
    Task AddAsync(DomainEvent @event, CancellationToken cancellationToken = default);
}
