# Manager API (Python)

REST API for user CRUD and JWT authentication, built with **Vertical Slice Architecture**, FastAPI, SQLAlchemy (async), and PostgreSQL.

## Overview

- User CRUD (create, read, update, delete) and search by name/email
- Login and refresh token against the `User` table (email + Argon2)
- JWT Bearer auth on protected `/api/v1/users/*` routes
- Problem Details for failures; typed success bodies per slice
- Alembic migrations and bootstrap path for the first user

## Architecture (VSA)

Layout under `python/src/`:

| Folder | Responsibility |
|--------|----------------|
| `features/{entity}/` | One module per use case + entity, errors, cache keys, events |
| `common/` | `Result`, messaging, Problem Details, `Clock` |
| `database/` | SQLAlchemy engine/session, models, Alembic |
| `authentication/` | JWT, user context, Argon2 hasher |

Legacy packages (`domain/`, `application/`, `infrastructure/`, `api/`) remain until cutover.

## Stack

| Concern | Library |
|--------|---------|
| HTTP | FastAPI + Uvicorn |
| Validation | Pydantic |
| ORM | SQLAlchemy 2 async + asyncpg |
| Migrations | Alembic (async) |
| Passwords | argon2-cffi |
| JWT | PyJWT |
| Tooling | uv, ruff, mypy, pytest, coverage |

## Requirements

- Python ≥ 3.12
- [uv](https://docs.astral.sh/uv/)
- PostgreSQL 14+ (runtime / Alembic)

## Install

```bash
cd python
uv sync
cp .env.example .env
# Edit .env: set DATABASE_URL and JWT_SECRET
```

## Configuration

| Variable | Required | Description |
|----------|----------|-------------|
| `DATABASE_URL` | yes | PostgreSQL URL |
| `JWT_SECRET` | yes | HMAC secret for signing tokens |
| `JWT_ISSUER` | no | Default `Manager.API` |
| `JWT_AUDIENCE` | no | Default `Manager.API` |
| `JWT_HOURS_TO_EXPIRE` | no | Default `1` |
| `JWT_REFRESH_DAYS_TO_EXPIRE` | no | Default `7` |

Never commit real secrets. `.env` is gitignored.

## Database migrations

```bash
uv run alembic upgrade head
# or
poe migrate
```

## Run the API

```bash
uv run uvicorn app.main:app --reload --app-dir src
# or
poe run
```

- Docs: http://localhost:8000/docs
- Health: `GET /health`

## HTTP contract

**Success:** typed JSON from the slice response model (`access_token`, `access_token_expires`, `refresh_token`, `refresh_token_expires` on login/refresh) or `204 No Content`.

**Failure:** Problem Details with `code` extension:

| Code | When |
|------|------|
| `Users.NotFound` | User id/email not found |
| `Users.EmailConflict` | Duplicate email |
| `Users.Validation` | Business validation failure |
| `Users.BootstrapNotAllowed` | Bootstrap when users exist |
| `Auth.InvalidCredentials` | Login failure |
| `Auth.InvalidRefreshToken` | Refresh failure |

### Routes

| Method | Path | Auth |
|--------|------|------|
| GET | `/health` | — |
| POST | `/api/v1/auth/login` | no |
| POST | `/api/v1/auth/refresh` | no (refresh token body) |
| POST | `/api/v1/users/bootstrap` | no (empty users table only) |
| POST | `/api/v1/users` | JWT |
| PUT | `/api/v1/users/{id}` | JWT |
| DELETE | `/api/v1/users/{id}` | JWT |
| GET | `/api/v1/users/{id}` | JWT |
| GET | `/api/v1/users` | JWT |
| GET | `/api/v1/users/by-email?email=` | JWT |
| GET | `/api/v1/users/search-by-name?name=` | JWT |
| GET | `/api/v1/users/search-by-email?email=` | JWT |

### Bootstrap (first user)

```bash
curl -X POST http://localhost:8000/api/v1/users/bootstrap \
  -H 'Content-Type: application/json' \
  -d '{"name":"Admin","email":"admin@example.com","password":"ChangeMe123"}'
```

### Login and refresh

```http
POST /api/v1/auth/login
{ "email": "admin@example.com", "password": "..." }

POST /api/v1/auth/refresh
{ "refreshToken": "..." }
```

Use `Authorization: Bearer <access_token>` on protected user routes.

## Tests

```bash
uv run pytest tests -v --tb=short
# or
poe test
```

```bash
poe lint
poe typecheck
poe check
```

## Project structure

```
python/
  src/
    features/          users/, auth/ — one module per slice
    common/            Result, problem responses, Clock
    database/          session, models
    authentication/    JWT, hasher, user context
    app/               FastAPI app + feature registry
  tests/features/
  alembic/
  pyproject.toml
```

## Conventions

- One use case = one module; handlers use `AsyncSession` directly (no repositories)
- Map `Result` failures to Problem Details in routes
- `Clock` in handlers; domain events + cache invalidation on mutations
- Run `/vsa-review` before committing slice changes

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Missing `DATABASE_URL` / `JWT_SECRET` | Copy `.env.example` → `.env` |
| Bootstrap returns 403 | User already exists |
| `401` on user routes | Send Bearer token from login |

---

© 2026
