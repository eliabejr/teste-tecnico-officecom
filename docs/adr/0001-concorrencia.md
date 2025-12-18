# ADR 0001 — Estratégia de Concorrência

## Contexto
Este teste técnico implementa uma API de transações financeiras (depósito/saque/compra) onde:
- Integridade do saldo é requisito crítico em cenários concorrentes.
- Compras devem responder em até 1,5s (meta de performance) e o sistema deve suportar milhares de requisições simultâneas.
- A arquitetura deve suportar múltiplas instâncias da API (escalabilidade horizontal).
- Garantir que operações concorrentes na mesma conta não resultem em perda de dados ou inconsistências.

## Decisão

### Abordagem Híbrida (Distributed Lock + Optimistic Locking + Retry)

Estratégia em camadas combinando Distributed Lock (Redis) + Optimistic Locking + Retry Logic, organizado da seguinte forma:

Localização no código:
- `src/BCBGames.Infrastructure/Repositories/AccountRepository.cs` em `GetByIdForUpdateAsync` (Distributed Lock)
- `src/BCBGames.Domain/Entities/Account.cs` campo `Version` com `[ConcurrencyCheck]` (Optimistic Locking)
- `src/BCBGames.Infrastructure/Repositories/UnitOfWork.cs` em `OrchestrateAsync` (Retry Logic)

Implementação em três camadas:

1. Distributed Lock (Redis): Adquire lock distribuído antes de ler a conta para atualização
   - Coordena múltiplas instâncias da API
   - TTL de 30 segundos com timeout de 10 segundos para aquisição
   - Invalida cache antes de ler do banco para garantir dados atualizados

2. Optimistic Locking: Campo `Version` incrementado a cada modificação
   - Entity Framework detecta conflitos via `DbUpdateConcurrencyException`
   - Evita lost updates mesmo se o distributed lock expirar prematuramente
   - Fornece camada adicional de proteção

3. Retry Logic: Backoff exponencial em caso de `ConcurrencyException`
   - Delay inicial de 5ms, máximo de 100ms
   - Permite recuperação de conflitos transitórios
   - Implementado em `UnitOfWork.OrchestrateAsync`

Por que esta abordagem?

Distributed Lock permite múltiplas instâncias trabalhando em conjunto sem perder consistência. Apesar da possibilidade de existirem conflitos ao usar o Optimistic Locking, a junção desta abordagem com o redis garante integridade mesmo em falhas parciais, pois a retry logic trata conflitos temporários de maneira automática.

O objetivo é garantir que o cenário do requisito (100 compras simultâneas de R$1) seja determinístico: todas as 100 transações são registradas e o saldo final é R$0

### Unit of Work

Cada operação de escrita é executada dentro de uma transação ACID através de `UnitOfWork.OrchestrateAsync`: ou saldo e transação persistem juntos, ou rollback.

### Tratamento de Erros

Exceções de domínio são convertidas para códigos HTTP apropriados via `GlobalExceptionMiddleware` para tornar os contratos da API previsíveis.

## Alternativas Consideradas

### 1. Pessimistic Locking (SELECT FOR UPDATE)
- Prós: Serialização garantida no banco, sem necessidade de retry.
- Contras: Pode gargalar em testes de altíssima concorrência, pode causar deadlocks.
- Decisão: Rejeitado - não atende requisito de múltiplas instâncias.

### 2. Optimistic Locking puro + retries
- Prós: Maior paralelismo sem bloquear; bom para contencioso baixo.
- Contras: Sob alta concorrência pode gerar muitos conflitos e respostas 409, exigindo muito retry/backoff. Em múltiplas instâncias, muitos retries podem impactar performance.
- Decisão: Rejeitado - estratégia híbrida oferece melhor garantia de consistência.

### 3. Distributed Lock puro
- Prós: Coordena múltiplas instâncias da API de forma simples.
- Contras: Se o lock expirar prematuramente, pode haver race conditions. Sem camada de proteção adicional.
- Decisão: Rejeitado - estratégia híbrida oferece maior garantia de consistência.

## Consequências

### Positivas

- ✅ Escalabilidade horizontal: Múltiplas instâncias da API podem trabalhar em conjunto
- ✅ Consistência garantida: Duas camadas de proteção (Distributed Lock + Optimistic Locking)
- ✅ Resiliência: Retry logic trata conflitos transitórios automaticamente
- ✅ Performance: Cache Redis reduz carga no banco para consultas (quando não há escrita concorrente)

### Negativas

- ⚠️ Latência adicional: Overhead de adquirir distributed lock (tipicamente < 10ms)
- ⚠️ Possíveis retries: Em alta concorrência na mesma conta, pode haver alguns retries
- ⚠️ Cache invalidation: Invalidação de cache em cada escrita reduz benefício do cache para leituras frequentes após escritas

### Mitigações

- TTL do lock: 30 segundos é suficiente para maioria das operações, com renovação opcional via `ExtendAsync`
- Retry limitado: Backoff exponencial previne muitos retries consecutivos
- Cache ainda efetivo: Para cenários de leitura pura (consultas), cache continua trazendo benefícios significativos

### Métricas Observadas

- Testes de concorrência (100 requisições simultâneas) passam consistentemente
- Saldo final correto após todas as transações
- Número de transações criadas = número de requisições (sem perdas)
- Performance dentro das metas (compras < 1,5s, consultas < 500ms)
