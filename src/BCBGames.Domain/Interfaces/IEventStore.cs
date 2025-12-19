using BCBGames.Domain.Events;

namespace BCBGames.Domain.Interfaces;

public interface IEventStore
{
    Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default);
}
