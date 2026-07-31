# Agente Python

Instruções para desenvolvimento Python neste projeto. Aplica-se ao trabalhar em `python/` ou arquivos `.py`.

## Arquitetura (VSA)

Layout sob `python/src/`:

| Pasta | Responsabilidade |
|-------|------------------|
| `features/{entity}/` | Um módulo por slice (request/command, validator, handler, rota, response) + entidade, `{entity}_errors`, `{entity}_cache_keys`, eventos |
| `common/` | `Result`, protocolos de messaging, Problem Details, `Clock` |
| `database/` | engine/session, modelos SQLAlchemy, Alembic |
| `authentication/` | JWT, contexto de usuário, hasher Argon2 |
| `authorization/` | Helpers de política de acesso (ex.: `admin_resource_access`) |

Código de aplicação fica em `features/`, `common/`, `database/`, `authentication/`, `authorization/` e `app/` — pacotes em camadas legados foram removidos no cutover (Task 17).

## Checklist obrigatório

1. **Slice**: um caso de uso = um módulo em `features/{entity}/{use_case}.py`.
2. **Handler**: usa `AsyncSession` direto — **sem** camada de repositório.
3. **Erros**: `Result` com códigos `{Feature}.{Reason}`; rota mapeia para Problem Details (sem envelope `ResultSchema`).
4. **Validação**: Pydantic/validator no comando; queries sem validator de entrada.
5. **Mutações**: disparar evento de domínio antes do commit; invalidar cache com `{entity}_cache_keys`.
6. **Tempo**: `Clock` / provider compartilhado — nunca `datetime.now(UTC)` em handlers.
7. **Isolamento**: não importar handler/validator/rota de outra feature; só tipos de domínio compartilhados.
8. **Descoberta**: registry único de módulos de feature — sem wiring manual por endpoint.

## Segurança — Users (admin CRUD)

A feature **users** é gerenciamento administrativo de recursos: rotas exigem JWT (`get_current_subject`), mas handlers **não** filtram linhas pelo subject do token. O contexto de usuário existe para recursos futuros com ownership e para claims; hoje não escopa linhas de User. Ver `authorization/admin_resource_access`.

## Stack

uv, poe, hatchling, ruff, mypy, pyright, Pydantic, FastAPI, SQLAlchemy async, Alembic, pytest.

## Skills Python

Invocar conforme a tarefa:

- `/python-vertical-slice` – novos slices, estrutura VSA (skill `python-separation-concerns`)
- `/python-type-hinting` – type hints, ruff, mypy, pyright
- `/python-fastapi` – FastAPI, rotas, Depends
- `/python-namespaces` – src layout, pacotes, imports
- `/python-pyproject` – pyproject.toml, uv, poe
- `/vsa-review` – auditoria de convenções antes do commit
