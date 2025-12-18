# ADR 0003: Abordagem CQRS com MediatR

## Contexto

Conforme especificado nos requisitos, o padrão CQRS é desejável e mencionado como diferencial na seção "Diferenciais (Bônus)".

### Situação Anterior

A arquitetura original utilizava uma abordagem tradicional com Services que misturavam operações de leitura e escrita:

- AccountService: Criava contas e realizava consultas (GetAccount, GetBalance, GetStatement)
- TransactionService: Processava transações (Deposit, Withdraw, Purchase) e consultava transações individuais

### Motivação para Mudança

1. Requisitos do Teste: CQRS mencionado como diferencial desejável
2. Separação de Responsabilidades: Separar claramente operações de escrita (Commands) de leitura (Queries)
3. Escalabilidade: Permitir otimizações independentes para leitura e escrita
4. Manutenibilidade: Código mais organizado e fácil de entender
5. Testabilidade: Handlers isolados são mais fáceis de testar

## Decisão

Adotar CQRS utilizando MediatR como mediator pattern para desacoplar controllers dos handlers de comandos e queries.


### Alternativas Consideradas

#### 1. CQRS sem MediatR (Implementação Manual)
- Prós: Sem dependências externas, controle total
- Contras: Mais código boilerplate, sem pipeline behaviors prontos, mais trabalho de manutenção
- Decisão: Rejeitado - MediatR oferece valor significativo com pouco overhead

#### 2. Manter Services Tradicionais
- Prós: Abordagem mais simples, menos mudanças
- Contras: Não atende requisito de CQRS e está menos preparado para escalabilidade
- Decisão: Rejeitado - não atende requisitos do teste técnico

#### 3. CQRS com Event Sourcing
- Prós: Rastreabilidade completa, possibilidade de reconstruir estado
- Contras: Complexidade significativa, overkill para requisitos atuais, overhead de infraestrutura
- Decisão: Rejeitado para esta fase - pode ser considerado em caso de implementação de Event Sourcing

## Consequências

### Positivas

- ✅ Pipeline Behaviors: Facilita adicionar logging, validação, autorização de forma centralizada
- ✅ Desacoplamento: Controllers não dependem de implementações específicas
- ✅ Testabilidade: Handlers podem ser testados isoladamente
- ✅ Atende Requisitos: Implementa CQRS como diferencial do teste técnico

### Negativas

- ⚠️ Complexidade Adicional: Mais arquivos, mais estrutura para entender inicialmente
- ⚠️ Curva de Aprendizado: Equipe precisa entender MediatR e padrão CQRS
- ⚠️ Impacto na Performance: MediatR adiciona uma camada indireta (impacto mínimo)
- ⚠️ Dependência Externa: Adiciona dependência do MediatR ao projeto

### Mitigações

1. Documentação: Esta ADR documenta as decisões e estrutura
2. Convenções: Estrutura consistente facilita navegação
3. Performance: Impacto do MediatR é desprezível comparado a I/O no banco
4. Dependência: MediatR é biblioteca madura e amplamente utilizada na comunidade .NET

## Implementação

### Arquivos Criados

#### Commands
1. `Commands/CreateAccount/CreateAccountCommand.cs` + `CreateAccountHandler.cs`
2. `Commands/Deposit/DepositCommand.cs` + `DepositHandler.cs`
3. `Commands/Withdraw/WithdrawCommand.cs` + `WithdrawHandler.cs`
4. `Commands/Purchase/PurchaseCommand.cs` + `PurchaseHandler.cs`

#### Queries
1. `Queries/GetAccount/GetAccountQuery.cs` + `GetAccountHandler.cs`
2. `Queries/GetBalance/GetBalanceQuery.cs` + `GetBalanceHandler.cs`
3. `Queries/GetStatement/GetStatementQuery.cs` + `GetStatementHandler.cs`
4. `Queries/GetTransaction/GetTransactionQuery.cs` + `GetTransactionHandler.cs`

#### Behaviors
1. `Behaviors/LoggingBehavior.cs`

#### Modificações
1. `Controllers/AccountsController.cs` - Atualizado para usar `IMediator`
2. `Controllers/TransactionsController.cs` - Atualizado para usar `IMediator`
3. `Program.cs` - Configuração do MediatR

#### Removidos
1. `Services/AccountService.cs` - Substituído por Commands/Queries
2. `Services/TransactionService.cs` - Substituído por Commands/Queries
3. `Interfaces/IServices.cs` - Não mais necessário

### Migração de Lógica

A lógica de negócio foi preservada integralmente, apenas reorganizada:

- AccountService.CreateAccountAsync → CreateAccountHandler
- AccountService.GetAccountAsync → GetAccountHandler
- AccountService.GetBalanceAsync → GetBalanceHandler
- AccountService.GetStatementAsync → GetStatementHandler
- TransactionService.DepositAsync → DepositHandler
- TransactionService.WithdrawAsync → WithdrawHandler
- TransactionService.PurchaseAsync → PurchaseHandler
- TransactionService.GetTransactionAsync → GetTransactionHandler
