# Task 10 Report: RegisterBootstrap slice (both stacks)

## Status: DONE

## Summary

Implemented `POST /api/v1/users/bootstrap` (anonymous) on both stacks. Handler rejects when any user exists (`Users.BootstrapNotAllowed`), otherwise creates user with hashed password, raises `UserCreatedDomainEvent`, invalidates `users:all` cache, and returns `{ id, name, email }`.

## TDD Evidence

1. **Failing phase:** Added handler, validator, and endpoint tests (.NET + Python) before implementation.
2. **Implementation:** `RegisterBootstrap.cs`, `register_bootstrap.py`, registry wiring, isolated .NET web factory for endpoint tests.
3. **Passing phase:**
   - `dotnet test Manager.Vsa.sln --filter FullyQualifiedName~RegisterBootstrap` → **12 passed**
   - `dotnet test Manager.Vsa.sln` → **57 passed**
   - `uv run pytest tests/features/users/test_register_bootstrap_*.py -v --no-cov` → **11 passed**
   - `uv run pytest tests/features/ -v --no-cov` → **38 passed**

## Commits

| SHA | Subject |
|-----|---------|
| `925d90e` | feat: VSA RegisterBootstrap slice |

## Files Created / Modified

| Action | Path |
|--------|------|
| Create | `dotnet/src/Manager.Api/Features/Users/RegisterBootstrap.cs` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Features/Users/RegisterBootstrapHandlerTests.cs` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Features/Users/RegisterBootstrapValidatorTests.cs` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Features/Users/RegisterBootstrapEndpointTests.cs` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Features/Users/RegisterBootstrapWebApplicationFactory.cs` |
| Create | `python/src/features/users/register_bootstrap.py` |
| Modify | `python/src/app/registry.py` |
| Create | `python/tests/features/conftest.py` |
| Modify | `python/tests/features/auth/conftest.py` |
| Create | `python/tests/features/users/test_register_bootstrap_handler.py` |
| Create | `python/tests/features/users/test_register_bootstrap_endpoint.py` |

## Self-Review

- **Parity:** Same route, anonymous, validation rules (name 2–80, email valid max 180, password 8–30), bootstrap guard, cache invalidation.
- **.NET:** `ValidationEndpointFilter`, `HybridCache.RemoveAsync(UserCacheKeys.All)`, domain event on entity after save.
- **Python:** `AppCache.remove(UserCacheKeys.ALL)`, domain `User` entity raises event after persist.
- **Alembic:** Not added — bootstrap persistence tests use SQLite `create_all`; refresh_token columns already in `UserModel`.

## Concerns

### 1. .NET endpoint test isolation

`RegisterBootstrapWebApplicationFactory` uses a per-class GUID in-memory DB so bootstrap tests do not pollute Login/Refresh shared factory. `ClearUsersAsync` clears state within that isolated DB when the success test runs after the “users exist” test.

### 2. Python shared fixtures

Moved VSA DB fixtures to `tests/features/conftest.py` so auth and users feature tests share them without `pytest_plugins` in a nested conftest.

## Test Commands

```bash
cd dotnet && dotnet test Manager.Vsa.sln --filter FullyQualifiedName~RegisterBootstrap
cd python && uv run pytest tests/features/users/test_register_bootstrap_handler.py tests/features/users/test_register_bootstrap_endpoint.py -v --no-cov
```

**Result:** All passed.

---

## Quality Fix (2026-07-30)

### Status: DONE

### Summary

Addressed Important Task 10 review findings: Python handler now raises `UserCreatedDomainEvent` on `User.from_persistence` after commit (Id set), strips name/email in handler and endpoint, and handler test asserts domain event. Added Python validator test for email length > 180. Added .NET `[Collection("RegisterBootstrap")]` for endpoint test serialization.

### Tests

- `dotnet test Manager.Vsa.sln --filter FullyQualifiedName~RegisterBootstrap` → **12 passed**
- `uv run pytest tests/features/users/test_register_bootstrap_handler.py tests/features/users/test_register_bootstrap_endpoint.py -v --no-cov` → **12 passed**

### Commits

| SHA | Subject |
|-----|---------|
| `ee68e94` | fix: RegisterBootstrap Python event/Id parity and input normalization |

### Changes

| Action | Path |
|--------|------|
| Modify | `python/src/features/users/register_bootstrap.py` |
| Modify | `python/tests/features/users/test_register_bootstrap_handler.py` |
| Create | `dotnet/tests/Manager.Vsa.Tests/Features/Users/RegisterBootstrapCollection.cs` |
| Modify | `dotnet/tests/Manager.Vsa.Tests/Features/Users/RegisterBootstrapEndpointTests.cs` |
