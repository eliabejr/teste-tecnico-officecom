# Arquitetura (visão global)

Este documento descreve a arquitetura do projeto. Use o [Mermaid](https://mermaid.live/) para visualizar.

## Nível 1 — Contexto

```mermaid
flowchart LR
  Client[Client / Integrador] -->|HTTP/JSON| Api[BCBGames.API]

  subgraph External[Infra externa]
    Pg[(PostgreSQL)]
    Redis[(Redis)]
    Kafka[(Kafka)]
  end

  Api -->|EF Core| Pg
  Api -->|cache + idempotency| Redis

  Api -->|OutboxPublisherService publica eventos| Kafka
  Kafka -->|KafkaConsumerService consome| Api

  Api -->|health checks| Pg
  Api -->|health checks| Redis
```

## Nível 2 — Containers

```mermaid
flowchart TB
  subgraph API[BCBGames.API]
    Controllers[Controllers]
    Middleware[GlobalExceptionMiddleware]
  end

  subgraph APP[BCBGames.Application]
    MediatR[MediatR]
    Commands[Commands (Handlers)]
    Queries[Queries (Handlers)]
    Behaviors[Pipeline Behaviors (Logging)]
  end

  subgraph DOMAIN[BCBGames.Domain]
    Entities[Entities: Account, Transaction]
    Events[Domain Events]
    Interfaces[Interfaces: IUnitOfWork, Repos, IOutboxService, IEventStore, etc.]
    Exceptions[Domain Exceptions]
  end

  subgraph INFRA[BCBGames.Infrastructure]
    DI[DependencyInjection]
    Db[EF Core: AppDbContext]
    Repos[Repositories + UnitOfWork]
    Cache[RedisCacheService]
    Idem[IdempotencyService]
    Outbox[OutboxService]
    OutboxWorker[OutboxPublisherService (Hosted)]
    EventStore[KafkaEventStore (Producer)]
    Consumer[KafkaConsumerService (Hosted)]
    Lock[RedisDistributedLockService]
  end

  subgraph EXT[Dependências]
    Pg[(PostgreSQL)]
    Redis[(Redis)]
    Kafka[(Kafka)]
  end

  Controllers --> MediatR
  Middleware --> Controllers

  MediatR --> Behaviors
  MediatR --> Commands
  MediatR --> Queries

  Commands --> Entities
  Commands --> Events
  Commands --> Interfaces
  Queries --> Interfaces

  DI --> Db
  DI --> Repos
  DI --> Cache
  DI --> Idem
  DI --> Outbox
  DI --> OutboxWorker
  DI --> EventStore
  DI --> Consumer
  DI --> Lock

  Repos --> Db
  Cache --> Redis
  Idem --> Redis
  Lock --> Redis
  Db --> Pg

  Outbox --> Db
  OutboxWorker --> Db
  OutboxWorker --> EventStore
  EventStore --> Kafka
  Consumer --> Kafka
  Consumer --> Repos
```

## Nível 3 — Fluxo

```mermaid
sequenceDiagram
  autonumber
  participant C as Client
  participant API as BCBGames.API
  participant MR as MediatR
  participant H as Command Handler
  participant UoW as UnitOfWork
  participant DB as PostgreSQL
  participant OB as OutboxMessages
  participant PUB as OutboxPublisherService
  participant ES as KafkaEventStore
  participant K as Kafka
  participant CON as KafkaConsumerService

  C->>API: POST (ex.: deposit/withdraw/purchase)
  API->>MR: Send(Command)
  MR->>H: Handle()
  H->>UoW: OrchestrateAsync(operation)
  UoW->>DB: BEGIN TRANSACTION
  H->>DB: INSERT Transaction / UPDATE Account
  H->>OB: INSERT OutboxMessage(event)
  UoW->>DB: COMMIT
  UoW-->>H: ok
  H-->>MR: Response DTO
  MR-->>API: Response DTO
  API-->>C: 200/201

  loop a cada ~5s (batch)
    PUB->>OB: SELECT pendentes
    PUB->>ES: Publish(event)
    ES->>K: Produce(topic, headers)
    PUB->>OB: UPDATE processed/attempts
  end

  K-->>CON: Consume(event)
  CON->>DB: Read Account / comparar saldo esperado
  CON-->>CON: Log divergências (se houver)
```
