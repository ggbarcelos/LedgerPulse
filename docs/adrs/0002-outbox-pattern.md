# ADR 0002 - Usar Outbox Pattern para integracao assincrona

## Status

Aceito.

## Contexto

O fluxo de lancamentos nao pode depender da disponibilidade imediata da consolidacao diaria.

## Decisao

Persistir eventos de integracao em uma tabela Outbox na mesma transacao do lancamento e processa-los assincronamente por um worker.

## Consequencias

- Reduz acoplamento temporal entre os contexts.
- Garante durabilidade dos eventos.
- Introduz defasagem eventual entre lancamento e consolidado.
