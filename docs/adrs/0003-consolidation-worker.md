# ADR 0003 - Processar consolidacao em worker dedicado

## Status

Aceito.

## Contexto

A consolidacao diaria exige processamento continuo sem bloquear a API principal.

## Decisao

Executar o processamento da Outbox em `LedgerPulse.ConsolidationWorker`, com polling configuravel.

## Consequencias

- Mantem a API de `Ledger` focada em escrita e consulta.
- Permite escalonar o processamento de consolidacao separadamente.
- Exige observabilidade sobre backlog da Outbox.
