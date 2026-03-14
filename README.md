# LedgerPulse

O LedgerPulse e uma base inicial em .NET 10 para um monolito modular com dois bounded contexts: `Ledger` e `DailyConsolidation`. A solucao usa PostgreSQL, EF Core, Docker, Blazor WebAssembly, Outbox Pattern e um worker dedicado para consolidacao assincrona do fluxo de caixa diario.

> “O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário cair.”

## Visao geral da arquitetura

A aplicacao foi estruturada como um monolito modular com separacao por camadas e por contexto de negocio:

- `Ledger`: recebe e lista lancamentos classificados como `Credit` ou `Debit`.
- `DailyConsolidation`: materializa os consolidados diarios com totais de credito, debito e saldo a partir de eventos persistidos na Outbox.
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

1. A API de `Ledger` recebe um novo lancamento de credito ou debito.
2. O aggregate `LedgerEntry` gera um evento de dominio.
3. O `DbContext` converte o evento em mensagem de Outbox na mesma transacao.
4. O worker consulta mensagens pendentes da Outbox.
5. O worker atualiza ou cria o consolidado diario em `DailyConsolidation`, acumulando creditos, debitos e saldo.
6. A mensagem e marcada como processada.

Esse desenho preserva a disponibilidade do fluxo de lancamentos mesmo quando a consolidacao estiver temporariamente indisponivel.

## Como rodar com Docker

### Pre-requisitos

- Docker Desktop
- .NET 10 SDK, caso queira rodar sem containers

### Subir todo o ambiente

Na raiz do projeto, execute:

```bash
docker compose up --build -d
```

Se voce quiser acompanhar os logs em primeiro plano:

```bash
docker compose up --build
```

Servicos expostos:

- API: `http://localhost:8080`
- Frontend: `http://localhost:8082`
- PostgreSQL: `localhost:5432`

No ambiente Docker, o frontend publica a aplicacao em `http://localhost:8082` e encaminha chamadas de `/api/*` para o servico `api` via nginx. Assim, o browser acessa frontend e API pela mesma origem visivel ao usuario.
O container do frontend aguarda a API responder em `/health` antes de concluir a inicializacao, evitando erros `502 Bad Gateway` logo apos o `docker compose up`.

### Primeira execucao ou reset completo do ambiente

Se voce quiser validar o projeto como ambiente virgem, removendo banco e dados persistidos:

```bash
docker compose down -v
docker compose up --build -d
```

Ao subir novamente, a API aplica automaticamente a migration inicial via `Database.MigrateAsync()`, criando o schema do zero com:

- `ledger_entries` com `description`, `amount`, `entryType`, `businessDate` e `createdAtUtc`
- `daily_ledger_summaries` com `totalCredits`, `totalDebits`, `entryCount` e `updatedAtUtc`
- `outbox_messages` para o processamento assincrono da consolidacao

Credenciais padrao do banco:

- Database: `ledgerpulse`
- User: `postgres`
- Password: `postgres`

## Como rodar localmente sem Docker

Na primeira execucao sem Docker, **nenhuma variavel de ambiente precisa ser definida**: as chaves de seguranca estao vazias por padrao, e a API ignora a validacao do header quando a chave nao esta configurada.

Opcionalmente, defina chaves distintas para os endpoints de escrita e operacao manual:

```bash
export ApiSecurity__LedgerWriteApiKey="ledger-write-local-key"
export ApiSecurity__ConsolidationProcessApiKey="consolidation-process-local-key"
```

Se voce configurar `ApiSecurity__LedgerWriteApiKey`, mantenha o mesmo valor em `src/LedgerPulse.Frontend/wwwroot/appsettings.Development.json` na chave `ApiKey` para os testes locais do frontend. Essa chave no frontend e apenas um valor de conveniencia para desenvolvimento local e **nao deve ser tratada como segredo de producao**.

Em terminais separados:

```bash
dotnet run --project src/LedgerPulse.Api
dotnet run --project src/LedgerPulse.ConsolidationWorker
dotnet run --project src/LedgerPulse.Frontend
```

A API e o worker usam por padrao a string de conexao local para PostgreSQL em `localhost:5432`.
Na primeira execucao, a infraestrutura aplica **EF Core Migrations** com `Database.MigrateAsync()`, criando o banco e as tabelas se ainda nao existirem.
Para bases antigas criadas pelo fluxo anterior com `EnsureCreated`, a inicializacao faz um baseline da migration inicial antes de aplicar novas migrations.
Ao rodar sem Docker, o frontend usa `http://localhost:5021/` como base da API e a API libera CORS explicitamente para a origem `http://localhost:5262`.

## Endpoints principais

### Ledger

- `GET /api/ledger/entries`
- `POST /api/ledger/entries`

Os endpoints `POST` exigem o header `X-Api-Key` apenas quando suas respectivas chaves estiverem configuradas:

- `POST /api/ledger/entries` -> `ApiSecurity:LedgerWriteApiKey`
- `POST /internal/daily-consolidation/process` -> `ApiSecurity:ConsolidationProcessApiKey`

As chaves devem ser fornecidas por variaveis de ambiente (nao ficam versionadas no frontend).

O endpoint de escrita aplica rate limiting com janela fixa para reduzir impacto de picos.
Os valores podem ser configurados em `RateLimiting:LedgerWrite` (`PermitLimit`, `WindowSeconds`, `QueueLimit`), com padrao de desenvolvimento ajustado para pico proximo de 50 rps.
Ha tambem um limite global por IP em `RateLimiting:Global` para reduzir abuso da superficie publica.

Exemplo de payload do `POST /api/ledger/entries`:

```json
{
  "description": "Venda no cartao",
  "amount": 1250.40,
  "entryType": "Credit",
  "businessDate": "2026-03-12"
}
```

Observacoes sobre o payload:

- `description`: texto livre do lancamento.
- `amount`: valor decimal positivo enviado para a API sem mascara, por exemplo `1250.40`.
- `entryType`: aceita `Credit` ou `Debit`.
- `businessDate`: opcional; deve ser enviada no formato ISO `yyyy-MM-dd`, por exemplo `2026-03-12`. Quando omitida, o sistema usa a data atual em UTC.

Observacao sobre a interface:

- Na tela web, o usuario escolhe a data em formato brasileiro (`dd/MM/aaaa`) por meio do calendario visual.
- O frontend converte essa data automaticamente para o formato ISO exigido pela API antes de enviar o payload.

Regras de negocio do lancamento:

- `amount` deve ser maior que zero.
- `entryType` define se o valor entra no caixa (`Credit`) ou sai do caixa (`Debit`).
- A moeda nao faz parte do contrato porque o sistema considera sempre Real Brasileiro (BRL).

Valores aceitos em `entryType`:

- `Credit`
- `Debit`

Exemplo de resposta do `GET /api/ledger/entries`:

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "businessDate": "2026-03-12",
    "description": "Venda no cartao",
    "amount": 1250.40,
    "entryType": "Credit",
    "createdAtUtc": "2026-03-12T14:30:00Z"
  }
]
```

### DailyConsolidation

- `GET /api/daily-consolidation/summaries`
- `POST /internal/daily-consolidation/process` (operacional/interno)

Cada item retornado em `GET /api/daily-consolidation/summaries` informa:

- `businessDate`
- `totalCredits`
- `totalDebits`
- `balance`
- `entryCount`
- `updatedAtUtc`

Exemplo de resposta:

```json
[
  {
    "businessDate": "2026-03-12",
    "totalCredits": 1250.40,
    "totalDebits": 300.00,
    "balance": 950.40,
    "entryCount": 2,
    "updatedAtUtc": "2026-03-12T23:59:59Z"
  }
]
```

O endpoint `POST /internal/daily-consolidation/process` usa chave dedicada (`ApiSecurity:ConsolidationProcessApiKey`) e possui rate limiting para evitar execucoes manuais em excesso.
Os valores desse endpoint ficam em `RateLimiting:ConsolidationProcess`.

O trigger manual da consolidacao foi removido da superficie publica `/api/*` e mantido apenas em rota interna para operacao.

Exemplo de resposta do `POST /internal/daily-consolidation/process`:

```json
{
  "processedMessages": 3,
  "ignoredMessages": 0
}
```

- `processedMessages`: quantidade de mensagens da Outbox aplicadas com sucesso ao consolidado diario.
- `ignoredMessages`: quantidade de mensagens com tipo desconhecido, marcadas como processadas sem retry.

## Testes e validacao

```bash
dotnet restore LedgerPulse.slnx
dotnet build LedgerPulse.slnx
dotnet test LedgerPulse.slnx
```

Se quiser recriar a migration inicial localmente:

```bash
dotnet ef migrations add InitialCreate --project src/LedgerPulse.Infrastructure --startup-project src/LedgerPulse.Api --output-dir Persistence/Migrations
```

## Documentacao complementar

Consulte a pasta `docs/` para ADRs iniciais e diagramas Mermaid com a visao arquitetural e o fluxo de consolidacao.
