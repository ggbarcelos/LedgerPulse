# Consolidation Flow

```mermaid
sequenceDiagram
    participant Client
    participant API as Ledger API
    participant DB as PostgreSQL
    participant Worker as Consolidation Worker
    participant Ops as Operations (manual)

    Client->>API: POST /api/ledger/entries
    API->>DB: INSERT ledger_entries + outbox_messages (same transaction)
    API-->>Client: 201 Created

    alt Worker auto-processing (poll loop)
        Worker->>DB: SELECT pending outbox_messages
        Worker->>DB: UPSERT daily_ledger_summaries
        Worker->>DB: UPDATE outbox_messages SET processed_on_utc
    else Manual trigger via internal endpoint
        Ops->>API: POST /internal/daily-consolidation/process (X-Api-Key)
        API->>DB: SELECT pending outbox_messages
        API->>DB: UPSERT daily_ledger_summaries
        API->>DB: UPDATE outbox_messages SET processed_on_utc
        API-->>Ops: 200 OK { processedMessages, ignoredMessages }
    end
````
