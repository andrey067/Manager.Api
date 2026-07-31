# Task 9 Report: RefreshToken slice (both stacks)

## Status: DONE

## Summary

Implemented `POST /api/v1/auth/refresh` on both stacks mirroring Login patterns. Handler hashes the incoming token, looks up user by hash with `RefreshTokenExpiresAt > clock.UtcNow`, returns `Auth.InvalidRefreshToken` on miss/expiry, otherwise rotates access + refresh tokens and persists new hash.

## TDD Evidence

1. **Failing phase:** Added handler, validator, and endpoint tests (.NET + Python) before implementation.
2. **Implementation:** `RefreshToken.cs`, `refresh_token.py`, registry wiring, `seed_user_with_refresh_token` test helper.
3. **Passing phase:**
   - `dotnet test Manager.Vsa.sln --filter FullyQualifiedName~RefreshToken` → **12 passed**
   - `dotnet test Manager.Vsa.sln` → **45 passed**
   - `uv run pytest tests/features/auth/test_refresh_token_*.py -v --no-cov` → **8 passed**
   - `uv run pytest tests/features/auth/ -v --no-cov` → **18 passed**

## Commits

| SHA | Subject |
|-----|---------|
| _(pending)_ | feat: VSA RefreshToken slice (.NET + Python) |

## Files Created / Modified

| Action | Path |
|--------|------|
| Create | `dotnet/src/Manager.Api/Features/Auth/RefreshToken.cs` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Features/Auth/RefreshTokenHandlerTests.cs` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Features/Auth/RefreshTokenValidatorTests.cs` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Features/Auth/RefreshTokenEndpointTests.cs` |
| Create | `python/src/features/auth/refresh_token.py` |
| Modify | `python/src/app/registry.py` |
| Modify | `python/tests/features/auth/conftest.py` |
| Create | `python/tests/features/auth/test_refresh_token_handler.py` |
| Create | `python/tests/features/auth/test_refresh_token_endpoint.py` |

## Self-Review

- **Parity:** Same route, anonymous, token response shape as Login; `Auth.InvalidRefreshToken` on invalid/expired.
- **Rotation:** Success path issues new access + refresh tokens and updates stored hash.
- **Clock:** .NET handler injects `IDateTimeProvider`; Python handler accepts optional `Clock` (defaults to `SystemClock` in production wiring).
- **Validation:** Empty refresh token → `Validation.Error` at endpoint.

## Concerns

### 1. Shared InMemory DB in .NET integration tests

`CustomWebApplicationFactory` uses a fixed InMemory database name; refresh endpoint test seeds `refresh@example.com` (not `user@example.com`) to avoid cross-test pollution with Login endpoint tests.

### 2. Python endpoint clock vs test fixture

Endpoint integration tests use `SystemClock` in handler DI while unit tests use `FixedClock`; safe today because seeded expiry is +7 days, but endpoint tests could inject a fixed clock later for determinism.

## Test Commands

```bash
cd dotnet && dotnet test Manager.Vsa.sln --filter FullyQualifiedName~RefreshToken
cd python && uv run pytest tests/features/auth/test_refresh_token_handler.py tests/features/auth/test_refresh_token_endpoint.py -v --no-cov
```

**Result:** All passed.
