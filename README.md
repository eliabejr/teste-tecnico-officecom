# Teste Técnico OfficeCom

Teste para a vaga de Sênior Backend na OfficeCom. Empresa fictícia BCB Games.

Autor: Eliabe Serafim Jr.

## Stack

- .NET 8
- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core
- Docker & Docker Compose

## Arquitetura

```
src/
├── BCBGames.API/
├── BCBGames.Application/
├── BCBGames.Domain/
└── BCBGames.Infrastructure/
```

A arquitetura segue os princípios de **Clean Architecture** com separação de responsabilidades da seguinte forma:
- API: Camada de apresentação (Controllers, Middleware)
- Application: Casos de uso (Commands/Queries com CQRS via MediatR)
- Domain: Entidades e regras de negócio
- Infrastructure: Persistência (EF Core, Redis), repositórios, cache

## Decisões Arquiteturais
Padrão ADR, armazenados na pasta `docs/adr.`

Detalhes neste [link](https://adr.github.io/).

## Setup Local

### Pré-requisitos
- Docker & Docker Compose
- .NET 8 SDK (ou, use docker)
- PostgreSQL (ou, use docker)

### Executando com Docker

```bash
# Sobe toda a stack (dotnet sdk, api, banco, redis, etc)
docker-compose up -d
```

### Executando Localmente

```bash
# 1. Inicia banco
docker-compose up -d postgres

# 2. Restaura pacotes
dotnet restore

# 3. Aplica migrations
cd src/BCBGames.API
dotnet ef migrations add InitialCreate --project ../BCBGames.Infrastructure
dotnet ef database update

# 4. Executa a API
dotnet run
```

Após executar, ficam disponíveis as seguintes URLs:
- API: http://localhost:8000
- Swagger: http://localhost:8000/swagger

## Endpoints

### Contas
POST `/api/accounts` -> Criar conta
GET `/api/accounts/{id}` -> Consultar conta
GET `/api/accounts/{id}/balance` -> Consultar saldo
GET `/api/accounts/{id}/statement` -> Extrato (paginado)

### Transações
POST `/api/transactions/deposit` -> Realizar depósito
POST `/api/transactions/withdraw` -> Realizar saque
POST `/api/transactions/purchase` -> Realizar compra
GET `/api/transactions/{id}` -> Consultar transação

### Monitoramento
GET `/health` -> Health check

## Exemplos

### Criar Conta
```bash
curl -X POST http://localhost:8000/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"ownerName": "Eliabe Serafim Jr", "initialBalance": 1000}'
```

### Realizar Compra
```bash
curl -X POST http://localhost:8000/api/transactions/purchase \
  -H "Content-Type: application/json" \
  -d '{"accountId": "xyz", "amount": 150.50, "merchant": "Amazon"}'
```

### Consultar Extrato
```bash
curl "http://localhost:8000/api/accounts/{id}/statement?page=1&pageSize=20"
```

## .ENVS

`ConnectionStrings__DefaultConnection`: String de conexão PostgreSQL. Padrão: localhost
`ASPNETCORE_ENVIRONMENT`: Ambiente (Development/Production). Padrão: Development

## Testes

### Unitários

```bash
dotnet test tests/BCBGames.UnitTests/BCBGames.UnitTests.csproj
```

### Integração

Os testes de integração usam Testcontainers.

```bash
dotnet test tests/BCBGames.IntegrationTests/BCBGames.IntegrationTests.csproj
```

### Todos

```bash
dotnet test
```

### Testes de Carga (k6)

Scripts para validar performance e concorrência:

```bash
# Teste de compras (valida P95 < 1.5s)
./scripts/k6-purchase.sh [VUS] [ITERATIONS_PER_VU]

# Teste de consultas (valida P95 < 500ms)
./scripts/k6-query.sh [VUS] [ITERATIONS_PER_VU]

# Teste de depósitos
./scripts/k6-deposit.sh [VUS] [ITERATIONS_PER_VU]

# Teste de saques
./scripts/k6-withdraw.sh [VUS] [ITERATIONS_PER_VU]
```

Os relatórios são salvos em `loadtests/reports/`.