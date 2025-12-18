# Teste Prático - Desenvolvedor Back-End Sênior
## API de Transações Financeiras de Alta Performance

### Objetivo
Desenvolver uma API REST robusta e escalável para gerenciar transações financeiras (depósitos, saques e compras) com garantias de integridade de saldo e alta performance, capaz de processar milhares de requisições simultâneas.

---

## 📋 Requisitos Funcionais

### 1. Gestão de Contas
- **Criar conta**:  Endpoint para criar uma nova conta de usuário com saldo inicial
- **Consultar saldo**: Endpoint para consultar o saldo atual de uma conta
- **Consultar extrato**: Endpoint para listar histórico de transações

### 2. Operações de Transação

#### 2.1 Depósito
- Adicionar valor ao saldo da conta
- Valor mínimo: R$ 0,01
- Registro da transação com timestamp

#### 2.2 Saque
- Deduzir valor do saldo da conta
- Validar saldo suficiente antes de processar
- Valor mínimo: R$ 0,01
- Registro da transação com timestamp

#### 2.3 Compra
- Deduzir valor do saldo da conta
- Validar saldo suficiente antes de processar
- **CRÍTICO**: Tempo de resposta máximo de 1,5 segundos
- Incluir descrição/merchant da compra
- Valor mínimo: R$ 0,01
- Registro da transação com timestamp

---

## 🎯 Requisitos Não-Funcionais

### Performance
- ✅ Cada operação de **compra** deve ser registrada em **no máximo 1,5 segundos**
- ✅ API deve suportar **milhares de requisições simultâneas**
- ✅ Tempo de resposta para consultas deve ser inferior a 500ms (P95)

### Integridade
- ✅ **Garantir integridade do saldo em cenários concorrentes**
- ✅ Prevenir condições de corrida (race conditions)
- ✅ Evitar saldo negativo (exceto se implementar cheque especial)
- ✅ Transações devem ser atômicas (ACID compliant)

### Escalabilidade
- ✅ Arquitetura preparada para escalonamento horizontal
- ✅ Stateless (sem estado na aplicação)
- ✅ Suporte a múltiplas instâncias da API

### Disponibilidade
- ✅ Tratamento adequado de erros
- ✅ Retry logic quando apropriado
- ✅ Circuit breaker (desejável)
- ✅ Health checks

---

## 🛠️ Stack Tecnológica Obrigatória

### Core
- **. NET 8 ou . NET 9** (última versão LTS)
- **C#** como linguagem principal
- **ASP.NET Core** para API REST

### Banco de Dados (escolher um)
- **SQL Server** ou **PostgreSQL** (recomendado para performance)
- Uso de **transações** e **locks adequados**
- Índices otimizados

### Extras (desejável)
- **Redis** para cache ou controle de concorrência
- **Message Broker** (RabbitMQ/Kafka) para processamento assíncrono
- **Docker** para containerização
- **xUnit** ou **NUnit** para testes

---

## 📐 Arquitetura Esperada

### Padrões Arquiteturais
- Clean Architecture ou Arquitetura em Camadas
- Repository Pattern
- CQRS (desejável para separar leitura/escrita)
- Unit of Work (se aplicável)

### Estrutura Sugerida
```
src/
├── FinancialTransactions. API/          # Camada de apresentação
├── FinancialTransactions. Application/  # Casos de uso e lógica de negócio
├── FinancialTransactions.Domain/       # Entidades e regras de domínio
├── FinancialTransactions.Infrastructure/ # Persistência e serviços externos
└── FinancialTransactions. Tests/        # Testes unitários e integração
```

---

## 🔐 Estratégias de Concorrência

O candidato deve implementar pelo menos uma das estratégias para garantir integridade: 

### Opção 1: Pessimistic Locking
- Uso de `SELECT FOR UPDATE` ou equivalente
- Bloqueio de linha no banco durante transação

### Opção 2: Optimistic Locking
- Controle de versão (Version/RowVersion)
- Retry em caso de conflito

### Opção 3: Distributed Lock
- Uso de Redis para locks distribuídos
- Timeout adequado para evitar deadlocks

### Opção 4: Event Sourcing (Avançado)
- Armazenamento de eventos ao invés de estado
- Reconstrução de saldo a partir de eventos

---

## 📊 Endpoints Esperados

### Contas
```
POST   /api/accounts              # Criar conta
GET    /api/accounts/{id}         # Consultar conta
GET    /api/accounts/{id}/balance # Consultar saldo
GET    /api/accounts/{id}/statement # Extrato
```

### Transações
```
POST   /api/transactions/deposit   # Realizar depósito
POST   /api/transactions/withdraw  # Realizar saque
POST   /api/transactions/purchase  # Realizar compra
GET    /api/transactions/{id}      # Consultar transação
```

### Monitoramento
```
GET    /health                     # Health check
GET    /metrics                    # Métricas (opcional)
```

---

## 📝 Modelos de Dados Sugeridos

### Account (Conta)
```csharp
{
  "id": "uuid",
  "accountNumber": "string",
  "ownerName": "string",
  "balance": "decimal",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "version": "int" // Para optimistic locking
}
```

### Transaction (Transação)
```csharp
{
  "id":  "uuid",
  "accountId": "uuid",
  "type": "Deposit|Withdraw|Purchase",
  "amount": "decimal",
  "description": "string",
  "balanceBefore": "decimal",
  "balanceAfter": "decimal",
  "status": "Pending|Completed|Failed",
  "createdAt": "datetime",
  "processedAt": "datetime"
}
```

---

## ✅ Critérios de Avaliação

### 1. Performance (Peso: 25%)
- [ ] Compras processadas em < 1,5s
- [ ] Consultas em < 500ms (P95)
- [ ] Suporte a carga simultânea (testes de stress)

### 2. Integridade de Dados (Peso: 30%)
- [ ] Saldo sempre consistente
- [ ] Sem race conditions
- [ ] Transações atômicas
- [ ] Testes de concorrência incluídos

### 3. Qualidade de Código (Peso: 20%)
- [ ] Clean Code
- [ ] SOLID principles
- [ ] Código testável
- [ ] Separação de responsabilidades

### 4. Escalabilidade (Peso: 15%)
- [ ] Arquitetura stateless
- [ ] Preparada para múltiplas instâncias
- [ ] Cache implementado onde apropriado
- [ ] Otimizações de banco de dados

### 5. Documentação e Boas Práticas (Peso: 10%)
- [ ] README com instruções claras
- [ ] Documentação da API (Swagger)
- [ ] Comentários quando necessário
- [ ] Configuração via environment variables

---

## 🧪 Testes Obrigatórios

### Testes Unitários
- Regras de negócio da camada de domínio
- Validações
- Cálculos de saldo

### Testes de Integração
- Endpoints da API
- Operações de banco de dados
- Cenários de concorrência

### Testes de Carga (Desejável)
- Usar **k6**, **JMeter** ou **Artillery**
- Simular 1000+ requisições simultâneas
- Medir tempo de resposta das compras

### Exemplo de Cenário de Teste de Concorrência
```
Dado:  Uma conta com saldo de R$ 100,00
Quando: 100 requisições simultâneas de compra de R$ 1,00 cada
Então: O saldo final deve ser R$ 0,00
E: Todas as 100 transações devem ser registradas
```

---

## 📦 Entregáveis

1. **Código fonte completo** em repositório Git
2. **README. md** contendo:
   - Instruções de setup e execução
   - Decisões arquiteturais
   - Estratégia de concorrência utilizada
   - Como executar os testes
   - Considerações de escalabilidade
3. **Docker Compose** (opcional mas recomendado)
4. **Coleção do Postman/Insomnia** ou documentação Swagger
5. **Relatório de testes de carga** (se executados)

---

## ⏱️ Prazo

- **Tempo estimado**: 5-7 dias
- **Tempo máximo**: 10 dias

---

## 🎁 Diferenciais (Bônus)

- ✨ Implementação de CQRS com MediatR
- ✨ Event Sourcing completo
- ✨ Observabilidade (logs estruturados, tracing, métricas)
- ✨ CI/CD pipeline configurado
- ✨ Kubernetes manifests
- ✨ Autenticação e autorização (JWT)
- ✨ Rate limiting
- ✨ Versionamento da API
- ✨ GraphQL como alternativa ao REST
- ✨ Implementação de padrão Saga para transações distribuídas

---

## 📞 Dúvidas e Esclarecimentos

O candidato pode entrar em contato para: 
- Esclarecimentos sobre requisitos ambíguos
- Validação de premissas técnicas
- Discussão sobre trade-offs arquiteturais

---

## 🚨 Observações Importantes

1. **Integridade é prioridade**: Preferimos uma solução mais lenta mas correta do que rápida e inconsistente
2. **Documente suas decisões**: Explique por que escolheu determinada abordagem
3. **Testes são essenciais**: Código sem testes não será considerado completo
4. **Simplicidade vs Complexidade**: Use a solução mais simples que atenda aos requisitos

---

**Boa sorte!  🚀**

Estamos ansiosos para ver sua solução e discutir suas escolhas técnicas na entrevista de apresentação. 