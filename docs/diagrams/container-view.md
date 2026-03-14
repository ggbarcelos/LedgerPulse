# Container View

```mermaid
flowchart TB
    Browser[Browser]
    Frontend["Blazor WASM Frontend\n(nginx :80 / Docker :8082\nDevKestrel :5262)"]
    Api["LedgerPulse.Api\n(Docker :8080 / Dev :5021)"]
    Worker[LedgerPulse.ConsolidationWorker]
    Postgres[(PostgreSQL :5432)]

    Browser -->|"HTTP requests"| Frontend
    Frontend -->|"proxies /api/* & /health\n(nginx reverse proxy)"| Api
    Api -->|"reads/writes"| Postgres
    Worker -->|"polls outbox\nwrites summaries"| Postgres
```

> **Nota Docker:** o container `frontend` executa um script `wait-for-api.sh` que aguarda a API responder em `/health` antes de iniciar o nginx, evitando erros `502 Bad Gateway` logo apos `docker compose up`.
> **Nota local (sem Docker):** o frontend usa `http://localhost:5021/` como `ApiBaseUrl` (definido em `wwwroot/appsettings.Development.json`) e a API libera CORS para a origem `http://localhost:5262`.
