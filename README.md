# LedgerPulse

O LedgerPulse e uma base inicial em .NET 10 para um monolito modular com dois bounded contexts: `Ledger` e `DailyConsolidation`. A solucao usa PostgreSQL, EF Core, Docker, Blazor WebAssembly, Outbox Pattern e um worker dedicado para consolidacao assincrona.

> “O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário cair.”

## Visao geral da arquitetura

A aplicacao foi estruturada como um monolito modular com separacao por camadas e por contexto de negocio:

- `Ledger`: recebe e lista lancamentos.
- `DailyConsolidation`: materializa os consolidados diarios a partir de eventos persistidos na Outbox.
- `Outbox Pattern`: os eventos gerados no contexto `Ledger` sao gravados na mesma transacao do lancamento.
- `ConsolidationWorker`: processa mensagens pendentes da Outbox e atualiza os consolidados.
- `Frontend`: interface Blazor WASM consumindo as APIs REST.

As APIs REST sao separadas logicamente em dois grupos:

- `GET/POST /api/ledger/*`
- `GET/POST /api/daily-consolidation/*`

## Estrutura do projeto

```text
LedgerPulse/
|- src/
|  |- LedgerPulse.Api/
|  |- LedgerPulse.Application/
|  |- LedgerPulse.Domain/
|  |- LedgerPulse.Infrastructure/
|  |- LedgerPulse.ConsolidationWorker/
|  \- LedgerPulse.Frontend/
|- tests/
|  |- LedgerPulse.UnitTests/
|  \- LedgerPulse.IntegrationTests/
|- docs/
|  |- adrs/
|  \- diagrams/
|- docker-compose.yml
\- README.md
```

## Fluxo de consolidacao

1. A API de `Ledger` recebe um novo lancamento.
2. O aggregate `LedgerEntry` gera um evento de dominio.
3. O `DbContext` converte o evento em mensagem de Outbox na mesma transacao.
4. O worker consulta mensagens pendentes da Outbox.
5. O worker atualiza ou cria o consolidado diario em `DailyConsolidation`.
6. A mensagem e marcada como processada.

Esse desenho preserva a disponibilidade do fluxo de lancamentos mesmo quando a consolidacao estiver temporariamente indisponivel.

## Como rodar com Docker

### Pre-requisitos

- Docker Desktop
- .NET 10 SDK, caso queira rodar sem containers

### Subir todo o ambiente

```bash
docker compose up --build
```

Servicos expostos:

- API: `http://localhost:8080`
- Frontend: `http://localhost:8082`
- PostgreSQL: `localhost:5432`

Credenciais padrao do banco:

- Database: `ledgerpulse`
- User: `postgres`
- Password: `postgres`

## Como rodar localmente sem Docker

Em terminais separados:

```bash
dotnet run --project src/LedgerPulse.Api
dotnet run --project src/LedgerPulse.ConsolidationWorker
dotnet run --project src/LedgerPulse.Frontend
```

A API e o worker usam por padrao a string de conexao local para PostgreSQL em `localhost:5432`.

## Endpoints principais

### Ledger

- `GET /api/ledger/entries`
- `POST /api/ledger/entries`

Exemplo de payload:

```json
{
  "description": "Payment received",
  "amount": 1250.40,
  "currency": "BRL",
  "businessDate": "2026-03-12"
}
```

### DailyConsolidation

- `GET /api/daily-consolidation/summaries`
- `POST /api/daily-consolidation/process`

O endpoint `process` permite disparar manualmente o processamento da Outbox, embora o worker ja execute esse trabalho em background.

## Testes e validacao

```bash
dotnet restore LedgerPulse.slnx
dotnet build LedgerPulse.slnx
dotnet test LedgerPulse.slnx
```

## Documentacao complementar

Consulte a pasta `docs/` para ADRs iniciais e diagramas Mermaid com a visao arquitetural e o fluxo de consolidacao.
