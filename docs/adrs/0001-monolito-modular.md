# ADR 0001 - Adotar monolito modular

## Status

Aceito.

## Contexto

O desafio pede dois bounded contexts, APIs REST separadas, consistencia operacional e simplicidade de deploy.

## Decisao

Adotar um monolito modular com separacao por camadas e organizacao interna por contexto de negocio.

## Consequencias

- Simplifica operacao e deploy inicial.
- Permite evolucao para extracao futura de servicos.
- Mantem isolamento logico entre `Ledger` e `DailyConsolidation`.
