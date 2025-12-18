# ADR 0002 — Testes

## Contexto

Precisamos de uma estratégia de testes que cubra todos os cenários de ação do projeto de forma confiável.

## Decisão

### Teste em Três Níveis

#### 1. Testes Unitários 

Decisão: Focar em regras do domínio e cálculos de saldo.

Localização: `tests/BCBGames.UnitTests/`

Motivo:
- Feedback rápido durante desenvolvimento
- Valida invariantes essenciais (ex.: não permitir saldo negativo; mínimo de valor em transação, etc.)
- Testa lógica de negócio isoladamente

Exemplos testados:
- Criação de conta com validações
- Operações de crédito e débito
- Validação de saldo suficiente
- Criação e finalização de transações

#### 2. Testes de Integração

Decisão: Validar endpoints e persistência usando PostgreSQL via Testcontainers.

Localização: `tests/BCBGames.IntegrationTests/`

Motivo:
- Exercita o fluxo completo (controller → handler → EF Core → PostgreSQL)
- Permite validar concorrência
- Valida integração entre camadas
- Não usa mocks

Cenários cobertos:
- Criação e consulta de contas
- Operações de transação (depósito, saque, compra)
- Validações de endpoints
- Testes de concorrência: 100 requisições simultâneas validando integridade de saldo

#### 3. Testes de Carga

Decisão: Scripts k6 parametrizáveis para validar performance e concorrência sob carga.

Localização: 
- `loadtests/k6/` - Scripts k6
- `scripts/k6-*.sh` - Scripts bash para execução

Scripts disponíveis:
- `purchase-concurrency.js` - Testes de compras (valida P95 < 1,5s)
- `query-performance.js` - Testes de consultas (valida P95 < 500ms)
- `deposit-concurrency.js` - Testes de depósitos
- `withdraw-concurrency.js` - Testes de saques

Motivo:
- Executa em qualquer máquina/CI sem instalar k6 localmente (via Docker)
- Permite medir latência e falhas para operações críticas
- Valida metas de performance definidas nos requisitos
- Simula carga realista (1000+ requisições simultâneas)

Thresholds configurados:
- Taxa de falhas: < 1%
- Compras: P95 < 1500ms
- Consultas: P95 < 500ms

## Alternativas Consideradas

### Testes de carga com JMeter ou outras ferramentas
- Prós: Ferramentas maduras e poderosas
- Contras: Mais complexas, requerem instalação, configuração mais verbosa
- Decisão: Rejeitado - k6 é mais simples, roda em Docker, tem sintaxe JavaScript mais acessível

### Testes de carga integrados aos testes de integração
- Prós: Tudo em um lugar
- Contras: Testes de integração ficam lentos, difícil isolar problemas, métricas menos claras
- Decisão: Rejeitado - separar permite executar testes rápidos frequentemente e testes de carga sob demanda

## Consequências

### Positivas

- ✅ Cobertura completa: Regras de negócio, integração e performance são testadas
- ✅ Validação de performance: Testes de carga validam requisitos não-funcionais

### Negativas

- ⚠️ Complexidade: Manter três níveis de testes requer mais organização
- ⚠️ Recursos: Testes de carga podem consumir recursos significativos

### Mitigações

- **Testes de carga opcionais**: Testes de carga não bloqueiam CI, podem ser executados sob demanda ou em schedule
- **Docker ubíquo**: Docker está presente em desenvolvimento e CI, não é barreira real

