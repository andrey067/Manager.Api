# Final VSA Migration Fix Report

**Date:** 2026-07-30  
**Scope:** Critical/Important findings from whole-branch review

## Summary

| Item | Status | Notes |
|------|--------|-------|
| Python Alembic refresh columns | Fixed | New revision `20260730234500` |
| README contract accuracy | Fixed | dotnet + python |
| .NET JWT key hardening | Fixed | Empty key in appsettings; fail-fast outside Testing |
| Domain event order | Skipped | Id is DB-generated; Raise after SaveChanges is intentional |

## 1. Python Alembic — refresh token columns

**File:** `python/alembic/versions/20260730234500_add_user_refresh_token_columns.py`

Adds to `User` table (matching `python/src/database/models.py` and .NET `InitialVsa`):

- `refresh_token_hash` — `VARCHAR(128)`, nullable
- `refresh_token_expires_at` — `timestamp with time zone`, nullable

**Chain:** `20250301000000` → `20260730234500` (head)

**Upgrade/downgrade:** Verified via `alembic history`; downgrade drops columns in reverse order.

## 2. README accuracy

### dotnet/README.md

| Before | After |
|--------|-------|
| Bootstrap returns `403` | Returns `400` |
| Login JSON field `email` | `login` |
| Error code `Users.Validation` | `Validation.Error` |
| Tests use Testcontainers | EF Core InMemory (no Docker) |
| Docker required for tests | Removed |

### python/README.md

| Before | After |
|--------|-------|
| Login JSON field `email` | `login` |
| Error code `Users.Validation` | `Validation.Error` |
| Bootstrap returns `403` | `400` (`Users.BootstrapNotAllowed`) |

## 3. .NET JWT defaults

**`appsettings.json`:** `Jwt:Key` set to empty string `""` (no committed signing key).

**`Program.cs`:**

- `ValidateJwtKey()` runs at startup when environment is not `Testing`.
- Throws `InvalidOperationException` if key is missing or shorter than 32 characters.
- Removed fallback `DEV_ONLY_REPLACE_WITH_USER_SECRETS_KEY_32+` from JWT Bearer configuration.

**Tests:** `CustomWebApplicationFactory` and other test factories continue to inject `Jwt:Key` via in-memory configuration under `Testing` environment.

## 4. Domain event order — skipped

`CreateUser` / `RegisterBootstrap` (both stacks) call `Raise` / `raise_event` **after** `SaveChanges` / `commit` because `User.Id` is database-generated. Moving Raise before persistence would emit events without a stable Id. No code change; documented here per review guidance.

## 5. Intentionally unchanged

- Authorization/ownership model (any JWT can manage users) — learning API design.
- Exception-in-cache-factory pattern — deferred.

## Verification

```bash
cd python && uv run pytest --tb=short -q --no-cov
# 114 passed

cd dotnet && dotnet test Manager.Vsa.sln
# 118 passed
```

## Commit

```
fix: Alembic refresh columns, JWT key hardening, README contract
```
