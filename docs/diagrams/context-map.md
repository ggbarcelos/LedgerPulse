# Context Map

```mermaid
flowchart LR
    Ledger[Ledger]
    Outbox[(Outbox)]
    Daily[DailyConsolidation]
    Worker[ConsolidationWorker]

    Ledger --> Outbox
    Outbox --> Worker
    Worker --> Daily
```
