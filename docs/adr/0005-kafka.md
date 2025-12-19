# ADR 0005: Kafka Consumer como Monitor de Integridade (e base para replay)

## Contexto

Com a adoção do modelo  proposto na ADR 0004, o `KafkaConsumerService` não é responsável por “aplicar projeções” no banco.
As mudanças de estado já acontecem dentro da transação do Postgres e ficam disponíveis para leitura imediatamente.

Ainda assim, Kafka continua sendo valioso como:

- Log de auditoria e trilha de eventos
- Canal de integração (atual/futuro)
- Fonte para detecção de inconsistências entre o “evento publicado” e o “estado atual persistido”

### Situação Anterior
- Consumer consumia eventos e aplicava projeções via AccountProjection
- Era responsável por persistir Account e Transaction no banco
- Idempotência garantida via IdempotencyService

## Decisão

Transformar o `KafkaConsumerService` em um monitor de integridade que:
1. Consome eventos do Kafka
2. Compara estado esperado (do evento) com estado atual (PostgreSQL)
3. Loga divergências encontradas

### Verificações Realizadas
- Account existe para o evento processado
- Balance atual corresponde ao BalanceAfter do evento
- (Possível extensão) Transaction foi persistida corretamente

## Alternativas Consideradas

### 1. Remover Consumer Completamente
- Prós: Menos código, arquitetura mais simples
- Contras: Perde capacidade de detectar inconsistências, perde log de eventos
- Decisão: Rejeitado - monitoramento tem valor operacional

### 2. Manter Consumer Aplicando Projeções
- Prós: Redundância de persistência
- Contras: Conflitos de versão, complexidade, operações duplicadas
- Decisão: Rejeitado - causa mais problemas que resolve

### 3. Consumer para Replay/Rebuild
- Prós: Permite reconstruir estado a partir de eventos
- Contras: Complexidade adicional, não é requisito do teste
- Decisão: Adiado - pode ser implementado se houver tempo hábil

## Consequências

### Positivas
- Detecção proativa de inconsistências
- Preserva eventos no Kafka para auditoria
- Base para implementar replay/rebuild futuro

### Negativas
- Consumer consome recursos sem aplicar projeções
- Alertas podem gerar ruído se mal calibrados

### Mitigações
- Logs estruturados para análise
- Métricas de divergência podem ser exportadas para observabilidade

OBS: este consumer é intencionalmente “read-only” em relação ao estado do domínio: ele **não** corrige divergências automaticamente, apenas observa e registra.
