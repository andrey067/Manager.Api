# Manager API (Python)

REST API for user CRUD and JWT authentication, built with **Clean Architecture**, FastAPI, SQLAlchemy (async), and PostgreSQL.

## Overview

- User CRUD (create, read, update, delete) and search by name/email
- Login against the `User` table (email + Argon2 password verify)
- JWT Bearer auth on all `/api/v1/users/*` routes
- Domain validation, domain notifications, and consistent JSON error responses
- Alembic migrations and a bootstrap path for the first user

## Architecture

```
api (FastAPI routers, schemas, DI)
  → application (use cases, ports)
    → domain (entities, validators, repository protocols)
  ← infrastructure (SQLAlchemy, Argon2, PyJWT, config)
shared (mediator, notifications, Optional)
```

Dependency rule: domain has no outward dependencies; application depends only on domain/ports; infrastructure and api implement ports.

## Stack

| Concern | Library |
|--------|---------|
| HTTP | FastAPI + Uvicorn |
| Validation | Pydantic (`EmailStr` + `email-validator`) |
| ORM | SQLAlchemy 2 async + asyncpg |
| Migrations | Alembic (async) |
| Passwords | argon2-cffi |
| JWT | PyJWT |
| Tooling | uv, ruff, mypy, pytest, coverage |

## Requirements

- Python ≥ 3.12
- [uv](https://docs.astral.sh/uv/)
- PostgreSQL 14+ (runtime / Alembic)
- Tests use in-memory SQLite (`aiosqlite`) — Docker is **not** required for the suite

## Install

```bash
cd python
uv sync
cp .env.example .env
# Edit .env: set DATABASE_URL and JWT_SECRET
```

## Configuration / environment variables

| Variable | Required | Description |
|----------|----------|-------------|
| `DATABASE_URL` | yes | PostgreSQL URL (`postgresql://` or `postgresql+psycopg2://`; converted to `asyncpg` at runtime) |
| `JWT_SECRET` | yes | HMAC secret for signing tokens (long random string) |
| `JWT_ISSUER` | no | Default `Manager.API` |
| `JWT_AUDIENCE` | no | Default `Manager.API` |
| `JWT_HOURS_TO_EXPIRE` | no | Default `1` |
| `JWT_ALGORITHM` | no | Default `HS256` |
| `HASH_TIME_COST` | no | Argon2 time cost (default `10`) |
| `HASH_LANES` | no | Argon2 parallelism (default `5`) |
| `HASH_MEMORY_COST` | no | Argon2 memory KiB (default `32768`) |
| `HASH_LENGTH` | no | Argon2 hash length (default `32`) |

Never commit real secrets. `.env` is gitignored; use `.env.example` as the template.

## Database migrations

```bash
# Apply migrations
uv run alembic upgrade head
# or
poe migrate

# Generate a new revision after model changes
uv run alembic revision --autogenerate -m "describe change"
```

Initial migration creates the `"User"` table with a unique constraint on `email` (`IX_User_Email`).

## Bootstrap first user

When the `User` table is empty you can either:

**A) HTTP bootstrap (open once)**

```bash
curl -X POST http://localhost:8000/api/v1/auth/bootstrap \
  -H 'Content-Type: application/json' \
  -d '{"name":"Admin User","email":"admin@example.com","password":"ChangeMe123"}'
```

**B) Seed script**

```bash
SEED_NAME="Admin User" SEED_EMAIL="admin@example.com" SEED_PASSWORD="ChangeMe123" \
  uv run python scripts/seed_admin.py
# or
poe seed
```

After the first user exists, bootstrap returns `403`. Further users are created via authenticated `POST /api/v1/users/create`.

## Run the API

```bash
uv run uvicorn app.main:app --reload --app-dir src
# or
poe run
```

- Docs: http://localhost:8000/docs
- Health: `GET /` → `{"status":"ok"}`
- DB health: `GET /health/db` → `200` when connected, `503` on failure

## Authentication

1. Bootstrap or seed a user.
2. `POST /api/v1/auth/login` with `{"login":"<email>","password":"<plain>"}`.
3. Use `Authorization: Bearer <token>` on all `/api/v1/users/*` routes.

Login verifies the password with Argon2 against the stored hash. Invalid credentials → `401`.

### Main endpoints

| Method | Path | Auth |
|--------|------|------|
| POST | `/api/v1/auth/login` | no |
| POST | `/api/v1/auth/bootstrap` | no (only if zero users) |
| POST | `/api/v1/users/create` | JWT |
| PUT | `/api/v1/users/update` | JWT |
| DELETE | `/api/v1/users/remove/{id}` | JWT |
| GET | `/api/v1/users/get/{id}` | JWT |
| GET | `/api/v1/users/get-all` | JWT |
| GET | `/api/v1/users/get-by-email?email=` | JWT |
| GET | `/api/v1/users/search-by-name?name=` | JWT |
| GET | `/api/v1/users/search-by-email?email=` | JWT |

## Tests and coverage

```bash
uv run python -m pytest tests -v --tb=short
# or
poe test
```

Coverage is enforced at **≥ 90%** (lines + branches) via `pytest-cov` in `pyproject.toml`.

Pyramid:

- **Unit**: domain validators/entities, use cases, handlers, hasher, JWT service
- **Integration**: repository against SQLite (async aiosqlite)
- **API**: httpx `AsyncClient` — bootstrap, login, JWT-protected CRUD, `401`s

```bash
poe lint
poe typecheck
poe check   # lint + typecheck
```

## Project structure

```
python/
  alembic/                 # migrations (async env.py)
  scripts/seed_admin.py
  src/
    api/                   # FastAPI app, routers, schemas, middleware
    application/           # use cases + ports (PasswordHasher, TokenService)
    domain/                # entities, validators, repository protocols
    infrastructure/        # DB, Argon2, JWT, config
    shared/                # mediator, notifications, Optional
  tests/
    api/ application/ domain/ infrastructure/ shared/
  .env.example
  alembic.ini
  pyproject.toml
```

## Conventions

- Absolute imports from package roots (`api`, `application`, `domain`, …)
- Application depends on **ports** (`PasswordHasher`, `TokenService`), not Argon2/PyJWT concretes
- Repository queries filter in SQL (`get_by_email`, `search_by_*`), not by loading all rows
- Plain passwords max length **80** (aligned with API); Argon2 hashes skip max-length checks
- `User.from_persistence` / `assign_hashed_password` for DB reconstruction

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `Missing required environment variable: DATABASE_URL` / `JWT_SECRET` | Copy `.env.example` → `.env` and set values |
| `EmailStr` / email-validator errors | Ensure `pydantic[email]` and `email-validator` are installed (`uv sync`) |
| Migration fails connecting | Check `DATABASE_URL`, network, and that the database exists |
| Bootstrap returns 403 | A user already exists — use login + authenticated create, or wipe the table |
| Tests need Docker | Not required — suite uses aiosqlite |
| `401` on user routes | Send `Authorization: Bearer <token>` from login |
| Password update always re-hashes | Expected when the plain password does not verify against the stored hash; unchanged password keeps the same hash |
