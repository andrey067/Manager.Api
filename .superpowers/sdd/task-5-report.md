# Task 5 Report: Database + User entity shell + health host

## Status: DONE

## Summary

Both stacks now boot a VSA host with `GET /health` → `200 {"status":"ok"}`, a persistable `User` entity/model shell, and injectable database access (`ApplicationDbContext` / async session factory). .NET registers HybridCache, Scrutor handler/endpoint scan, FluentValidation, JWT placeholder, and `IDateTimeProvider`. Health integration tests use EF InMemory (.NET) and direct ASGI (Python) — no Docker/Testcontainers required for this task.

## TDD Evidence

1. **Failing phase:** Added `HealthEndpointTests` + `CustomWebApplicationFactory` (.NET) and `test_health_vsa.py` (Python) before host/DB implementation.
2. **Implementation:** User entity, DbContext/configuration, migration, Program.cs DI; Python `features/users/entity.py`, `database/`, `app/main.py`; `pyproject.toml` package wiring.
3. **Passing phase:**
   - `dotnet test Manager.Vsa.sln --filter FullyQualifiedName~Health` → **1 passed**
   - `dotnet test Manager.Vsa.sln` → **10 passed**
   - `uv run pytest tests/api/test_health_vsa.py -v --no-cov` → **1 passed**

## Commits

| SHA | Subject |
|-----|---------|
| _(pending)_ | feat: VSA hosts with health, User persistence shell |

## Files Created / Modified

| Action | Path |
|--------|------|
| Create | `dotnet/src/Manager.Api/Features/Users/User.cs` |
| Create | `dotnet/src/Manager.Api/Database/ApplicationDbContext.cs` |
| Create | `dotnet/src/Manager.Api/Database/Configurations/UserConfiguration.cs` |
| Create | `dotnet/src/Manager.Api/Database/Migrations/20260730234201_InitialVsa.cs` |
| Modify | `dotnet/src/Manager.Api/Program.cs` |
| Modify | `dotnet/src/Manager.Api/appsettings.json` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Health/HealthEndpointTests.cs` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Health/CustomWebApplicationFactory.cs` |
| Create | `python/src/features/users/entity.py` |
| Create | `python/src/database/session.py`, `config.py`, `models.py` |
| Create | `python/src/app/main.py` |
| Create | `python/tests/api/test_health_vsa.py` |
| Modify | `python/pyproject.toml` |

## Self-Review

- **User mapping:** Matches legacy `UserMap` (table `User`, unique `email`, refresh token columns, column names/types).
- **Health:** No DB roundtrip on `/health`; factory uses InMemory DbContext override in Testing environment.
- **JWT:** Registered with configurable key; test factory injects fixed signing key.
- **Python:** VSA packages (`features`, `database`, `app`) added to hatch + ruff first-party; session factory injectable via `get_session_factory()` / `configure_engine()`.

## Concerns

### 1. No Alembic revision for VSA refresh-token columns

Python VSA `UserModel` includes refresh token fields; existing legacy Alembic revision only has base columns. New VSA migration can be added when VSA becomes the primary DB path.

### 2. DbContext skipped in Testing environment

`Program.cs` omits default Npgsql registration when `ASPNETCORE_ENVIRONMENT=Testing`; factory must always register InMemory (or test provider).

### 3. No User persistence integration tests yet

Entity shell only; handler/slice tests come in later tasks.

## Test Commands

```bash
cd dotnet && dotnet test Manager.Vsa.sln --filter FullyQualifiedName~Health
cd python && uv run pytest tests/api/test_health_vsa.py -v --no-cov
```

**Result:** All passed.
