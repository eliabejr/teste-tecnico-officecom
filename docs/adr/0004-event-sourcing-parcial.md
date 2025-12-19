# ADR 0004: Postgres como Fonte de Verdade + Outbox + Kafka como Event Log

## Contexto

O sistema precisa:

- Responder GET imediatamente após POST com dados consistentes (sem polling/delay artificial).
- Manter um log de eventos para auditoria, debug e possível replay futuro.
- Ser resiliente a falhas parciais ao integrar com Kafka (ex: banco commitou, mas o publish falhou).

### Situação atual

Hoje a arquitetura é transacional no PostgreSQL (estado atual).

## Decisão

Adotar PostgreSQL como fonte de verdade do estado atual e Outbox Pattern para publicar eventos no Kafka com consistência transacional no banco.

### Fluxo (simplificado)

1. API recebe request e executa um Command via MediatR.
2. Handler aplica regra de negócio no domínio e prepara:
   - `Account` atualizado
   - `Transaction` nova
   - `DomainEvent` (ex.: `DepositedEvent`, `WithdrawnEvent`, `PurchasedEvent`, `AccountCreatedEvent`)
3. Persistência via `UnitOfWork.OrchestrateAsync` + transação EF Core:
   - grava `Accounts`/`Transactions`
   - grava `OutboxMessages` (evento serializado, com `IdempotencyKey`)
4. Resposta é retornada após commit do Postgres.
5. `OutboxPublisherService` lê `OutboxMessages` pendentes em lote e publica no Kafka via `KafkaEventStore`.

## Alternativas Consideradas

### 1. Projeção assíncrona (eventual consistency “pura”)
- Prós: Event Sourcing clássico, desacopla escrita de leitura.
- Contras: `GET` após `POST` pode retornar 404/dados antigos; exige polling/delays; piora UX e fragiliza testes.
- Decisão: Rejeitado para o escopo atual.

### 2. Publicar no Kafka dentro do handler
- Prós: Eventos saem “imediatamente”.
- Contras: Não há atomicidade entre Postgres e Kafka; falhas geram divergência; exige compensações complexas.
- Decisão: Rejeitado.

### 3. Event Store como fonte de verdade
- Prós: Garantias e primitives nativas de Event Sourcing.
- Contras: Infra adicional, curva de aprendizado, custo operacional maior.
- Decisão: Rejeitado temporariamente para o escopo atual.

## Consequências

### Positivas

- ✅ Consistência imediata da API: leitura após escrita funciona sem polling.
- ✅ Publicação confiável: Outbox reduz risco de “banco commitou, Kafka não”.
- ✅ Observabilidade e auditoria: Kafka mantém o log de eventos publicado.
- ✅ Escalabilidade: publicação é assíncrona e em batch (config atual: polling 5s, batch 50, tentativas 5).

### Trade-offs

- ⚠️ Eventos no Kafka podem atrasar (polling), então consumidores externos não devem assumir “imediato”.
- ⚠️ Operação extra: tabela `OutboxMessages` + worker de publicação + limpeza/monitoramento.
- ⚠️ Exactly-once end-to-end não é garantido: temos idempotência (Outbox unique index e headers), mas a entrega Kafka é “at-least-once” na prática.

### Mitigações

- Idempotência:
  - `OutboxMessages` possui índice único em `(IdempotencyKey, EventType, AggregateId)`.
  - `KafkaEventStore` publica com headers (`idempotency-key`, `event-id`, `event-type`).
- Resiliência do publisher: tentativas com `Attempts` + `LastError` e log para intervenção manual quando excede max.
- Monitoramento de divergência: `KafkaConsumerService` (ADR 0005) compara “estado esperado pelo evento” com “estado atual no Postgres”.
