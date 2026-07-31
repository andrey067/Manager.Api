---
name: python-separation-concerns
description: Vertical Slice Architecture em Python no Manager API — features, common, database, authentication; um módulo por caso de uso. Use ao criar slices, módulos ou quando o usuário menciona VSA, vertical slice ou separação de responsabilidades.
---

# Vertical Slice Architecture (Python)

## Estrutura

```
python/src/
  features/
    users/   user.py, user_errors.py, user_cache_keys.py, create_user.py, ...
    auth/    login.py, refresh_token.py
  common/          Result, messaging, problem responses, Clock
  database/        session, models, Alembic
  authentication/  JWT, user context, hasher
```

## Novo slice

1. **Módulo único** `features/{entity}/{use_case}.py` contendo request/command, validator (comandos), handler, rota FastAPI e response model.
2. **Handler** recebe `AsyncSession` via dependência — consultas e commits diretos, sem repositório.
3. **Erros**: `Result` com códigos `"{Feature}.{Reason}"` em `{entity}_errors.py`; rota traduz para Problem Details.
4. **Query**: sem validator de forma; projetar para `Response` — nunca retornar entidade crua.
5. **Mutação**: evento de domínio antes do commit; invalidar chaves em `{entity}_cache_keys`.
6. **Tempo**: usar `Clock` — não `datetime.now(UTC)` em handlers.
7. **Registro**: exportar rota no registry de features (`app/registry.py`) — sem factory manual por endpoint.
8. **Testes**: handler (cada falha + happy path), validator por regra, endpoint com `AsyncClient`.

## Isolamento

- Feature pode usar `common`, `database`, `authentication` e tipos de domínio de outra feature (entidade, errors, events).
- **Nunca** importar handler, validator ou rota de outra feature.

## Proibido

- Camadas `domain` → `application` → `infrastructure` como fronteiras novas
- Interfaces de repositório, mediator DomainNotification, envelope `ResultSchema` em sucesso

## Revisão

Rodar `/vsa-review` no diff antes de commitar.
