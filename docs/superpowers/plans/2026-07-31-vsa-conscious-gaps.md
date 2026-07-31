# VSA Conscious Gaps Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the post-migration VSA gaps (ownership docs + Authorization folder, cache not-found without sentinel exceptions, domain-event conventions including auth mutations, Testcontainers for HTTP integration, and small validation/trim parity nits) without changing the locked HTTP contract.

**Architecture:** Keep both stacks as admin-style User management APIs (JWT required; any authenticated caller may CRUD any user). Fix Result/control-flow purity on cached reads, document identity-column Raise-after-Save as the Create/Bootstrap rule, raise domain events on Login/Refresh, wire Testcontainers for WebApplicationFactory suites, and align validation/trim with existing VSA patterns.

**Tech Stack:** .NET 10 Minimal APIs, EF Core, HybridCache, FluentValidation, xUnit, Testcontainers.PostgreSql; Python 3.12 FastAPI, SQLAlchemy async, AppCache, Pydantic, pytest, aiosqlite (handlers) + optional Postgres for parity docs.

## Global Constraints

- HTTP routes and Problem Details codes stay locked (see migration plan / AGENTS).
- Users feature remains **admin resource management**: do **not** filter mutations/queries by `IUserContext.UserId == resourceId`.
- No MediatR, no repositories, no cross-feature Command/Query/Handler imports.
- Expected failures return `Result` / `Result[T]` — no exceptions for business control flow (including cache miss / not-found).
- Mutations raise domain events on the aggregate; Create/Bootstrap may Raise **after** the Save/commit that assigns DB identity Id; Update/Remove/Login/Refresh Raise **before** Save/commit.
- Handler unit tests may keep EF InMemory / aiosqlite; HTTP integration factories that currently use InMemory must move to Testcontainers PostgreSQL (.NET). Python HTTP tests may keep aiosqlite unless a task says otherwise.
- Work both stacks in the same task when the gap is dual-stack; commit after each task.
- Solution for .NET work: `dotnet/Manager.Vsa.sln` only.

### Decisions locked for this plan

| Gap | Decision |
|-----|----------|
| Ownership | Document intentional admin CRUD; add `Authorization/` convention type; update `vsa-review` exemption for Users |
| Domain events Create/Bootstrap | Keep Raise-after-identity-Save; document in AGENTS + vsa-review |
| Cache not-found | Cache-aside / nullable factory + remove null entries; no sentinel exceptions |
| Testcontainers | Use for .NET WebApplicationFactory integration tests; keep InMemory for handler unit tests |
| Authorization folder | Add minimal shared convention marker (not a permission engine) |
| UpdateUser validation | Bind `Command` via `BindAsync` so `ValidationEndpointFilter<Command>` works |
| Python validators | Move shape rules onto Pydantic request models; delete hand-rolled `Validator` classes |
| Email trim | Trim email (and name where already trimmed in Python) in .NET FluentValidation transforms |
| Login/Refresh events | Add `UserLoggedInDomainEvent` / `UserTokenRefreshedDomainEvent`; Raise before SaveChanges |

---

## File structure (create / modify)

### .NET

```
dotnet/src/Manager.Api/
  Authorization/
    AdminResourceAccess.cs          # convention + XML docs (admin CRUD)
  Features/Users/
    GetUser.cs                      # no sentinel exception
    GetUserByEmail.cs
    CreateUser.cs                   # email trim in Validator
    RegisterBootstrap.cs            # email trim
    UpdateUser.cs                   # BindAsync + filter; email trim
    UserLoggedInDomainEvent.cs      # new (Auth uses Users domain events — OK)
    UserTokenRefreshedDomainEvent.cs
  Features/Auth/
    Login.cs                        # Raise before Save
    RefreshToken.cs
  Common/ValidationEndpointFilter.cs  # unchanged (UpdateUser binds Command)

dotnet/tests/Manager.Vsa.Tests/
  Infrastructure/
    PostgresFixture.cs              # Testcontainers shared fixture
    PostgresCollection.cs
  Features/Users/*WebApplicationFactory.cs  # use fixture connection string
  Health/CustomWebApplicationFactory.cs
  Features/Auth/* (endpoint factories if any)
  Features/Users/GetUserHandlerTests.cs     # assert no-exception path still works
  Features/Auth/LoginHandlerTests.cs        # domain event asserted
```

### Python

```
python/src/
  authorization/
    __init__.py
    admin_resource_access.py
  common/cache.py                   # do not cache None
  features/users/get_user.py
  features/users/get_user_by_email.py
  features/users/create_user.py     # Pydantic validation; drop Validator class
  features/users/register_bootstrap.py
  features/users/update_user.py
  features/auth/login.py            # raise_event before commit
  features/auth/refresh_token.py
  features/users/events.py          # UserLoggedIn / UserTokenRefreshed

python/tests/...                    # mirror assertions
```

### Docs / skills

```
dotnet/AGENTS.md, python/AGENTS.md
.cursor/skills/vsa-review/SKILL.md
README.md (short note on admin auth + Testcontainers for integration)
```

---

### Task 1: Document ownership + Authorization folder + vsa-review exemption

**Files:**
- Create: `dotnet/src/Manager.Api/Authorization/AdminResourceAccess.cs`
- Create: `python/src/authorization/__init__.py`
- Create: `python/src/authorization/admin_resource_access.py`
- Modify: `.cursor/skills/vsa-review/SKILL.md` (ownership + Raise identity bullets)
- Modify: `dotnet/AGENTS.md`, `python/AGENTS.md` (short “Admin Users API” note)
- Test: `dotnet/tests/Manager.Vsa.Tests/Authorization/AdminResourceAccessTests.cs`
- Test: `python/tests/authorization/test_admin_resource_access.py`

**Interfaces:**
- Consumes: nothing
- Produces: `AdminResourceAccess.IsAdminManagedResource` (static true for `"Users"`) — documentation-oriented helper used only in tests/docs, **not** called from handlers

- [ ] **Step 1: Write the failing .NET test**

Create `dotnet/tests/Manager.Vsa.Tests/Authorization/AdminResourceAccessTests.cs`:

```csharp
using FluentAssertions;
using Manager.Api.Authorization;

namespace Manager.Vsa.Tests.Authorization;

public class AdminResourceAccessTests
{
    [Fact]
    public void Users_IsAdminManaged_DoesNotRequireResourceOwnership()
    {
        AdminResourceAccess.IsAdminManagedFeature("Users").Should().BeTrue();
        AdminResourceAccess.RequiresResourceOwnership("Users").Should().BeFalse();
    }

    [Fact]
    public void UnknownFeature_DefaultsToOwnershipRequired()
    {
        AdminResourceAccess.RequiresResourceOwnership("Orders").Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test dotnet/Manager.Vsa.sln --filter FullyQualifiedName~AdminResourceAccessTests --no-restore 2>&1 | tail -20`  
(First time: `dotnet restore dotnet/Manager.Vsa.sln` then re-run.)  
Expected: FAIL — type or namespace `Manager.Api.Authorization` not found.

- [ ] **Step 3: Implement Authorization + docs**

`dotnet/src/Manager.Api/Authorization/AdminResourceAccess.cs`:

```csharp
namespace Manager.Api.Authorization;

/// <summary>
/// Manager API treats some features as admin resource management:
/// JWT is required, but handlers do not scope rows to <c>IUserContext.UserId</c>.
/// </summary>
public static class AdminResourceAccess
{
    private static readonly HashSet<string> AdminManagedFeatures =
        new(StringComparer.Ordinal) { "Users" };

    public static bool IsAdminManagedFeature(string featureName) =>
        AdminManagedFeatures.Contains(featureName);

    public static bool RequiresResourceOwnership(string featureName) =>
        !IsAdminManagedFeature(featureName);
}
```

`python/src/authorization/__init__.py`: empty or re-export.

`python/src/authorization/admin_resource_access.py`:

```python
from __future__ import annotations

_ADMIN_MANAGED = frozenset({"Users"})


def is_admin_managed_feature(feature_name: str) -> bool:
    return feature_name in _ADMIN_MANAGED


def requires_resource_ownership(feature_name: str) -> bool:
    return feature_name not in _ADMIN_MANAGED
```

`python/tests/authorization/test_admin_resource_access.py`:

```python
from authorization.admin_resource_access import (
    is_admin_managed_feature,
    requires_resource_ownership,
)


def test_users_is_admin_managed() -> None:
    assert is_admin_managed_feature("Users") is True
    assert requires_resource_ownership("Users") is False


def test_unknown_requires_ownership() -> None:
    assert requires_resource_ownership("Orders") is True
```

Update `.cursor/skills/vsa-review/SKILL.md` ownership bullet to:

```markdown
- Handlers acting on **user-owned** data enforce ownership: filter by `IUserContext.UserId` or return `UserErrors.Unauthorized()`. **Exception (Manager API):** the `Users` feature is admin-managed (`Authorization/AdminResourceAccess`); JWT via `.RequireAuthorization()` / `get_current_subject` is enough — do **not** require `UserId == route id`.
```

Update state-changes bullet to:

```markdown
- Commands that mutate state raise a domain event via `entity.Raise(...)` / `raise_event(...)`. For **existing** aggregates (Update/Remove/Login/Refresh), Raise **before** `SaveChangesAsync` / commit. For **Create/Bootstrap** with database-generated `long`/`int` Ids, Save/commit first to assign Id, then Raise with the persisted Id (before cache invalidation / return). Documented intentional — do not invent Guids solely to Raise earlier.
```

Add a short paragraph to `dotnet/AGENTS.md` and `python/AGENTS.md` under security: Users = admin CRUD; `IUserContext` is for future owned resources / claims, not for scoping User rows today.

- [ ] **Step 4: Run tests to verify they pass**

Run:

```bash
dotnet test dotnet/Manager.Vsa.sln --filter FullyQualifiedName~AdminResourceAccessTests
cd python && uv run pytest tests/authorization/test_admin_resource_access.py -v
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add dotnet/src/Manager.Api/Authorization python/src/authorization \
  dotnet/tests/Manager.Vsa.Tests/Authorization python/tests/authorization \
  .cursor/skills/vsa-review/SKILL.md dotnet/AGENTS.md python/AGENTS.md
git commit -m "$(cat <<'EOF'
docs: record admin Users ownership exemption and Authorization helpers

EOF
)"
```

---

### Task 2: Cache reads — Result without sentinel exceptions (.NET + Python)

**Files:**
- Modify: `dotnet/src/Manager.Api/Features/Users/GetUser.cs`
- Modify: `dotnet/src/Manager.Api/Features/Users/GetUserByEmail.cs`
- Modify: `python/src/common/cache.py`
- Modify: `python/src/features/users/get_user.py`
- Modify: `python/src/features/users/get_user_by_email.py`
- Test: existing `GetUserHandlerTests` / `GetUserByEmailHandlerTests` + Python equivalents; add “does not cache miss” assertions
- Test: `python/tests/common/test_cache.py` (create if missing)

**Interfaces:**
- Consumes: `HybridCache.GetOrCreateAsync` / `AppCache.get_or_set`
- Produces: same `Result<Response>` / `Result[Response]` contracts; factories return nullable and never throw for not-found

- [ ] **Step 1: Write failing Python cache test for “None is not stored”**

Create or extend `python/tests/common/test_cache.py`:

```python
import pytest

from common.cache import AppCache
from common.clock import Clock
from datetime import datetime, timezone


class FixedClock:
    def __init__(self, instant: datetime) -> None:
        self._instant = instant

    def utc_now(self) -> datetime:
        return self._instant


@pytest.mark.asyncio
async def test_get_or_set_does_not_cache_none() -> None:
    clock = FixedClock(datetime(2026, 7, 31, tzinfo=timezone.utc))
    cache = AppCache(clock=clock)  # type: ignore[arg-type]
    calls = {"n": 0}

    async def factory() -> None:
        calls["n"] += 1
        return None

    assert await cache.get_or_set("k", factory, ttl_seconds=60) is None
    assert await cache.get_or_set("k", factory, ttl_seconds=60) is None
    assert calls["n"] == 2
```

If `Clock` is a Protocol, prefer implementing it properly rather than `type: ignore`.

- [ ] **Step 2: Run Python test — expect FAIL**

Run: `cd python && uv run pytest tests/common/test_cache.py::test_get_or_set_does_not_cache_none -v`  
Expected: FAIL (`calls["n"] == 1` because None was cached) or import errors if file paths differ — adjust to match existing `Clock` API in `python/src/common/clock.py`.

- [ ] **Step 3: Fix AppCache to skip caching None**

In `python/src/common/cache.py`, after invoking the factory:

```python
        value = await self._invoke_factory(factory)
        if value is None:
            return None
        expires_at = now + timedelta(seconds=ttl_seconds)
        self._entries[key] = _CacheEntry(value=value, expires_at=expires_at)
        return value
```

- [ ] **Step 4: Rewrite GetUser / GetUserByEmail handlers (both stacks)**

**.NET `GetUser.Handler`** (same pattern in `GetUserByEmail` with email key/query):

```csharp
public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
{
    var response = await cache.GetOrCreateAsync(
        UserCacheKeys.ById(query.Id),
        async ct =>
        {
            return await db.Users
                .AsNoTracking()
                .Where(u => u.Id == query.Id)
                .Select(u => new Response(u.Id, u.Name, u.Email))
                .SingleOrDefaultAsync(ct);
        },
        cancellationToken: cancellationToken);

    if (response is null)
    {
        await cache.RemoveAsync(UserCacheKeys.ById(query.Id), cancellationToken);
        return Result.Failure<Response>(UserErrors.NotFound());
    }

    return Result.Success(response);
}
```

Delete `try/catch` and nested `UserNotFoundException`.

**Python `get_user.py` Handler.handle:**

```python
    async def handle(self, query: Query) -> Result[Response]:
        cache_key = UserCacheKeys.by_id(query.id)

        async def factory() -> Response | None:
            model = (
                await self._session.execute(
                    select(UserModel).where(UserModel.id == query.id)
                )
            ).scalar_one_or_none()
            if model is None:
                return None
            return Response(id=model.id, name=model.name, email=model.email)

        response = await self._cache.get_or_set(
            cache_key,
            factory,
            ttl_seconds=_CACHE_TTL_SECONDS,
        )
        if response is None:
            return Result.failure(UserErrors.not_found())
        return Result.success(response)
```

Remove `_UserNotFound`. Mirror in `get_user_by_email.py`.

- [ ] **Step 5: Extend .NET not-found test to prove miss is not sticky**

Add to `GetUserHandlerTests.cs`:

```csharp
    [Fact]
    public async Task Handle_NotFound_DoesNotPoisonCache_AllowsLaterSuccess()
    {
        var (handler, db, _) = await CreateSut();

        var miss = await handler.Handle(new GetUser.Query(1), CancellationToken.None);
        miss.IsFailure.Should().BeTrue();

        var hasher = new TestPasswordHasher();
        var user = User.Create("Later", "later@example.com", hasher.Hash("Password1!"));
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hit = await handler.Handle(new GetUser.Query(user.Id), CancellationToken.None);
        hit.IsSuccess.Should().BeTrue();
        hit.Value.Email.Should().Be("later@example.com");
    }
```

Mirror idea in Python get_user tests if present.

- [ ] **Step 6: Run tests**

```bash
dotnet test dotnet/Manager.Vsa.sln --filter "FullyQualifiedName~GetUser"
cd python && uv run pytest tests/common/test_cache.py tests/features/users/ -k "get_user" -v
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add dotnet/src/Manager.Api/Features/Users/GetUser.cs \
  dotnet/src/Manager.Api/Features/Users/GetUserByEmail.cs \
  dotnet/tests/Manager.Vsa.Tests/Features/Users/GetUserHandlerTests.cs \
  dotnet/tests/Manager.Vsa.Tests/Features/Users/GetUserByEmailHandlerTests.cs \
  python/src/common/cache.py python/src/features/users/get_user.py \
  python/src/features/users/get_user_by_email.py python/tests/common \
  python/tests/features/users
git commit -m "$(cat <<'EOF'
fix: return cache miss as Result without sentinel exceptions

EOF
)"
```

---

### Task 3: Domain events on Login / Refresh (+ document Create Raise-after-Save)

**Files:**
- Create: `dotnet/src/Manager.Api/Features/Users/UserLoggedInDomainEvent.cs`
- Create: `dotnet/src/Manager.Api/Features/Users/UserTokenRefreshedDomainEvent.cs`
- Modify: `dotnet/src/Manager.Api/Features/Auth/Login.cs`
- Modify: `dotnet/src/Manager.Api/Features/Auth/RefreshToken.cs`
- Modify: `dotnet/tests/Manager.Vsa.Tests/Features/Users/UserDomainEventsTests.cs`
- Modify: `dotnet/tests/Manager.Vsa.Tests/Features/Auth/LoginHandlerTests.cs`
- Modify: `dotnet/tests/Manager.Vsa.Tests/Features/Auth/RefreshTokenHandlerTests.cs`
- Create/Modify: `python/src/features/users/events.py`
- Modify: `python/src/features/auth/login.py`, `refresh_token.py`
- Modify: Python auth handler tests
- Docs already updated in Task 1 for Create/Bootstrap ordering — no code change required for Raise-after-Save on Create

**Interfaces:**
- Consumes: `User.SetRefreshToken`, `Entity.Raise` / `User.raise_event`
- Produces: `UserLoggedInDomainEvent(long Id)`, `UserTokenRefreshedDomainEvent(long Id)` (Python: `id: int`)

- [ ] **Step 1: Write failing domain event + handler tests (.NET)**

Extend `UserDomainEventsTests.cs`:

```csharp
    [Fact]
    public void UserLoggedInDomainEvent_HasId()
    {
        new UserLoggedInDomainEvent(11).Id.Should().Be(11);
    }

    [Fact]
    public void UserTokenRefreshedDomainEvent_HasId()
    {
        new UserTokenRefreshedDomainEvent(12).Id.Should().Be(12);
    }
```

In `LoginHandlerTests`, after a successful login assertion, add:

```csharp
        persisted.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserLoggedInDomainEvent>()
            .Which.Id.Should().Be(persisted.Id);
```

(Use the same `persisted` user tracked by the handler’s DbContext — reload if the test currently detaches.)

Mirror for Refresh → `UserTokenRefreshedDomainEvent`.

- [ ] **Step 2: Run tests — expect FAIL**

Run: `dotnet test dotnet/Manager.Vsa.sln --filter "FullyQualifiedName~UserLoggedIn|FullyQualifiedName~LoginHandler|FullyQualifiedName~RefreshTokenHandler"`  
Expected: FAIL — missing types / events not raised.

- [ ] **Step 3: Implement events + Raise before Save**

```csharp
// UserLoggedInDomainEvent.cs
namespace Manager.Api.Features.Users;
public sealed record UserLoggedInDomainEvent(long Id) : IDomainEvent;
```

```csharp
// UserTokenRefreshedDomainEvent.cs
namespace Manager.Api.Features.Users;
public sealed record UserTokenRefreshedDomainEvent(long Id) : IDomainEvent;
```

In `Login.Handler` after `SetRefreshToken`, **before** `SaveChangesAsync`:

```csharp
            user.Raise(new UserLoggedInDomainEvent(user.Id));
            await db.SaveChangesAsync(cancellationToken);
```

In `RefreshToken.Handler` after rotating refresh token fields:

```csharp
            user.Raise(new UserTokenRefreshedDomainEvent(user.Id));
            await db.SaveChangesAsync(cancellationToken);
```

Python `events.py` add dataclasses; in login/refresh handlers call `user.raise_event(...)` on the domain entity **before** `await session.commit()` (rehydrate or mutate the tracked model consistently with UpdateUser’s pattern in this codebase).

- [ ] **Step 4: Run tests — expect PASS**

```bash
dotnet test dotnet/Manager.Vsa.sln --filter "FullyQualifiedName~Auth|FullyQualifiedName~UserLoggedIn|FullyQualifiedName~UserTokenRefreshed|FullyQualifiedName~UserDomainEvents"
cd python && uv run pytest tests/features/auth -v
```

- [ ] **Step 5: Commit**

```bash
git add dotnet/src/Manager.Api/Features/Users/UserLoggedInDomainEvent.cs \
  dotnet/src/Manager.Api/Features/Users/UserTokenRefreshedDomainEvent.cs \
  dotnet/src/Manager.Api/Features/Auth python/src/features/auth python/src/features/users/events.py \
  dotnet/tests/Manager.Vsa.Tests/Features/Auth dotnet/tests/Manager.Vsa.Tests/Features/Users/UserDomainEventsTests.cs \
  python/tests/features/auth
git commit -m "$(cat <<'EOF'
feat: raise domain events on login and refresh token rotation

EOF
)"
```

---

### Task 4: .NET UpdateUser — BindAsync + ValidationEndpointFilter

**Files:**
- Modify: `dotnet/src/Manager.Api/Features/Users/UpdateUser.cs`
- Modify: `dotnet/tests/Manager.Vsa.Tests/Features/Users/UpdateUserEndpointTests.cs` (ensure 400 still works)

**Interfaces:**
- Consumes: `ValidationEndpointFilter<Command>`, FluentValidation `Validator`
- Produces: endpoint signature that binds `Command` directly (route `id` + JSON body)

- [ ] **Step 1: Write / adjust failing endpoint test if manual path is the only coverage**

Ensure `UpdateUserEndpointTests` has a case with invalid body (empty name) expecting Problem Details validation failure. If missing, add:

```csharp
    [Fact]
    public async Task Put_InvalidBody_ReturnsValidationProblem()
    {
        var client = /* authenticated client from factory */;
        var response = await client.PutAsJsonAsync("/api/v1/users/1", new { name = "", email = "a@b.com", password = "Password1!" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
```

(Adapt to the factory/helpers already used in that file.)

- [ ] **Step 2: Run test (may already pass via manual validation) — then refactor**

- [ ] **Step 3: Implement BindAsync and remove manual ValidateAsync from endpoint**

Replace nested `Request` + manual validation with:

```csharp
    public sealed record Command(long Id, string Name, string Email, string Password) : ICommand<Response>
    {
        public static async ValueTask<Command?> BindAsync(HttpContext httpContext, ParameterInfo parameter)
        {
            if (!long.TryParse(httpContext.Request.RouteValues["id"]?.ToString(), out var id))
                return null;

            var body = await httpContext.Request.ReadFromJsonAsync<Body>();
            if (body is null)
                return null;

            return new Command(id, body.Name, body.Email, body.Password);
        }

        private sealed record Body(string Name, string Email, string Password);
    }
```

Endpoint becomes:

```csharp
                    async (
                        Command command,
                        [FromServices] ICommandHandler<Command, Response> handler,
                        CancellationToken cancellationToken) =>
                    {
                        var result = await handler.Handle(command, cancellationToken);
                        return result.Match(Results.Ok, CustomResults.Problem);
                    })
                .AddEndpointFilter<ValidationEndpointFilter<Command>>()
                .RequireAuthorization()
                .WithTags(Tags.Users);
```

Add usings: `System.Reflection`, `Microsoft.AspNetCore.Http`. Keep `Validator` rules unchanged (Id > 0 if present).

- [ ] **Step 4: Run UpdateUser tests**

```bash
dotnet test dotnet/Manager.Vsa.sln --filter FullyQualifiedName~UpdateUser
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add dotnet/src/Manager.Api/Features/Users/UpdateUser.cs \
  dotnet/tests/Manager.Vsa.Tests/Features/Users/UpdateUserEndpointTests.cs
git commit -m "$(cat <<'EOF'
refactor: bind UpdateUser Command for ValidationEndpointFilter

EOF
)"
```

---

### Task 5: Python — Pydantic request validation (drop hand-rolled Validator classes)

**Files:**
- Modify: `python/src/features/users/create_user.py`
- Modify: `python/src/features/users/register_bootstrap.py`
- Modify: `python/src/features/users/update_user.py`
- Modify: corresponding `python/tests/features/users/test_*validator*` or endpoint tests

**Interfaces:**
- Consumes: Pydantic `Field`, `field_validator`
- Produces: same HTTP bodies; endpoint builds `Command` from validated request; handlers unchanged aside from receiving already-normalized strings

- [ ] **Step 1: Write failing tests that empty name on Create returns 422/problem**

Use existing endpoint tests; if validators are unit-tested against `Validator`, rewrite those tests to target Pydantic model validation:

```python
import pytest
from pydantic import ValidationError
from features.users.create_user import CreateUserRequest


def test_create_user_request_rejects_short_name() -> None:
    with pytest.raises(ValidationError):
        CreateUserRequest(name="A", email="a@b.com", password="Password1!")
```

- [ ] **Step 2: Run — may FAIL if constraints not on model yet**

- [ ] **Step 3: Move rules onto request models; delete `Validator` / `ValidationError` dataclasses**

Example for Create:

```python
from pydantic import BaseModel, EmailStr, Field, field_validator

class CreateUserRequest(BaseModel):
    name: str = Field(min_length=2, max_length=80)
    email: EmailStr = Field(max_length=180)
    password: str = Field(min_length=8, max_length=30)

    @field_validator("name", "email", mode="before")
    @classmethod
    def strip_strings(cls, value: object) -> object:
        return value.strip() if isinstance(value, str) else value
```

Endpoint: rely on FastAPI request validation; remove manual `Validator().validate(...)` branch. Mirror for bootstrap + update (`user_id` stays path param).

If `EmailStr` changes error shape vs old regex, update tests to accept 422 Problem Details from FastAPI — keep codes consistent where the app maps validation errors today.

- [ ] **Step 4: Run Python user feature tests**

```bash
cd python && uv run pytest tests/features/users -v
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add python/src/features/users/create_user.py \
  python/src/features/users/register_bootstrap.py \
  python/src/features/users/update_user.py \
  python/tests/features/users
git commit -m "$(cat <<'EOF'
refactor: validate user commands via Pydantic request models

EOF
)"
```

---

### Task 6: .NET email (and name) trim parity with Python

**Files:**
- Modify: `CreateUser.cs`, `RegisterBootstrap.cs`, `UpdateUser.cs` Validators
- Modify: `Login.cs` Validator (trim login/email)
- Test: validator tests asserting `"  a@b.com  "` normalizes

**Interfaces:**
- Consumes: FluentValidation `Transform`
- Produces: commands with trimmed Name/Email before handler runs (filter validates transformed command)

- [ ] **Step 1: Failing validator test**

In `CreateUserValidatorTests.cs`:

```csharp
    [Fact]
    public void Email_IsTrimmed()
    {
        var validator = new CreateUser.Validator();
        var result = validator.Validate(
            new CreateUser.Command("Ada", "  ada@example.com  ", "Password1!"));
        result.IsValid.Should().BeTrue();
        // Transform applies on validated instance — assert via Validate with pre-validate:
        var cmd = new CreateUser.Command("Ada", "  ada@example.com  ", "Password1!");
        validator.Validate(cmd);
        // Prefer: use Transform on RuleFor and assert through handler integration OR
        // TestValidate + custom assert that handler receives trim via endpoint filter.
    }
```

FluentValidation `Transform` mutates the working instance during validation when using the filter. Prefer an endpoint or handler-level test:

```csharp
    [Fact]
    public async Task Handle_TrimsEmail()
    {
        var (handler, db, _) = await CreateSut();
        var result = await handler.Handle(
            new CreateUser.Command("Ada", "  ada@example.com  ", "Password1!"),
            CancellationToken.None);
        // Without transform in handler, email stored with spaces — FAIL until Validator Transform
        // is applied in endpoint filter before Handle. So test the Validator:
        var v = new CreateUser.Validator();
        var cmd = new CreateUser.Command("  Ada  ", "  ada@example.com  ", "Password1!");
        var vr = v.Validate(cmd);
        vr.IsValid.Should().BeTrue();
        cmd.Email.Should().Be("ada@example.com"); // only if Command is class; records are immutable!
    }
```

Because `Command` is a `record`, **Transform cannot mutate it**. Use one of:

1. Change endpoint filter path to pass trimmed command:  
   `new Command(name.Trim(), email.Trim(), password)` inside a tiny `Normalize` before Handle, or  
2. Change `Command` properties to init-only class, or  
3. Trim inside handlers (acceptable for this nit; keeps records).

**Chosen approach for this plan:** trim in FluentValidation using `RuleFor(x => x.Email).Must(...)` is insufficient for mutation. **Trim at the start of each Handler** (Create/Bootstrap/Update/Login) and in Python keep strip (already present). For .NET Validators, add `.Must(e => e == e.Trim())` **or** simply trim in handlers:

```csharp
var email = command.Email.Trim();
var name = command.Name.Trim();
```

And add handler test:

```csharp
        var result = await handler.Handle(
            new CreateUser.Command("  Ada  ", "  ada@example.com  ", "Password1!"),
            CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        (await db.Users.SingleAsync()).Email.Should().Be("ada@example.com");
        (await db.Users.SingleAsync()).Name.Should().Be("Ada");
```

- [ ] **Step 2: Run — expect FAIL (email stored with spaces)**

- [ ] **Step 3: Trim in CreateUser / RegisterBootstrap / UpdateUser / Login handlers**

```csharp
            var name = command.Name.Trim();
            var email = command.Email.Trim();
```

Use `name`/`email` for conflict checks and persistence. Login: `command.Login.Trim()` for lookup.

- [ ] **Step 4: Run Create/Update/Login handler tests**

```bash
dotnet test dotnet/Manager.Vsa.sln --filter "FullyQualifiedName~CreateUser|FullyQualifiedName~RegisterBootstrap|FullyQualifiedName~UpdateUser|FullyQualifiedName~LoginHandler"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add dotnet/src/Manager.Api/Features/Users/CreateUser.cs \
  dotnet/src/Manager.Api/Features/Users/RegisterBootstrap.cs \
  dotnet/src/Manager.Api/Features/Users/UpdateUser.cs \
  dotnet/src/Manager.Api/Features/Auth/Login.cs \
  dotnet/tests/Manager.Vsa.Tests
git commit -m "$(cat <<'EOF'
fix: trim user name and email on command handlers

EOF
)"
```

---

### Task 7: Testcontainers for .NET WebApplicationFactory integration tests

**Files:**
- Create: `dotnet/tests/Manager.Vsa.Tests/Infrastructure/PostgresFixture.cs`
- Create: `dotnet/tests/Manager.Vsa.Tests/Infrastructure/PostgresCollection.cs`
- Modify: every `*WebApplicationFactory.cs` and `Health/CustomWebApplicationFactory.cs` to use Postgres + `EnsureCreated` or migrate
- Modify: endpoint test classes to `[Collection(PostgresCollection.Name)]`
- Keep: handler unit tests on EF InMemory
- Package: `Testcontainers.PostgreSql` already referenced — **use it** (do not remove)

**Interfaces:**
- Consumes: `Testcontainers.PostgreSql.PostgreSqlContainer`
- Produces: `PostgresFixture.ConnectionString` shared per test collection

- [ ] **Step 1: Write fixture + one failing collection wiring proof**

`PostgresFixture.cs`:

```csharp
using Testcontainers.PostgreSql;

namespace Manager.Vsa.Tests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("manager_vsa_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
```

Temporarily add a smoke test:

```csharp
public class PostgresFixtureSmokeTests
{
    [Fact]
    public void FixtureType_IsLoadable() =>
        typeof(PostgresFixture).FullName.Should().Contain("PostgresFixture");
}
```

- [ ] **Step 2: Run smoke — expect PASS once files compile; Docker required for later steps**

```bash
dotnet test dotnet/Manager.Vsa.sln --filter FullyQualifiedName~PostgresFixtureSmoke
```

- [ ] **Step 3: Convert `CreateUserWebApplicationFactory` to Postgres**

Pattern:

```csharp
public sealed class CreateUserWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CreateUserWebApplicationFactory(PostgresFixture postgres) =>
        _connectionString = postgres.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-32-chars-minimum!!",
                ["ConnectionStrings:ManagerAPIPostgres"] = _connectionString
            });
        });
        // Do NOT Replace with InMemory — use real Npgsql from Program DI.
        // Ensure schema: in factory, create scope and Database.EnsureCreated()
        // or Migrate() once per fixture in InitializeAsync helper.
    }
}
```

Prefer migrating schema once in `PostgresFixture.InitializeAsync` after start:

```csharp
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
    }
```

Endpoint test class:

```csharp
[Collection(PostgresCollection.Name)]
public class CreateUserEndpointTests : IClassFixture<CreateUserWebApplicationFactory>
```

But `IClassFixture<CreateUserWebApplicationFactory>` needs factory ctor injection of `PostgresFixture` — use:

```csharp
[Collection(PostgresCollection.Name)]
public class CreateUserEndpointTests
{
    private readonly HttpClient _client;

    public CreateUserEndpointTests(PostgresFixture postgres)
    {
        var factory = new CreateUserWebApplicationFactory(postgres);
        _client = factory.CreateClient();
    }
}
```

Or implement `IClassFixture` custom factory that also implements collection fixture consumer — follow xUnit docs: collection fixture + inject `PostgresFixture` into test class ctor, construct factory there.

Apply the same to RegisterBootstrap, RemoveUser, UpdateUser, Get*, Auth endpoint factories, and Health factory.

**Isolation:** each test class should use a unique database name **or** wipe tables in `InitializeAsync` per class. Simplest: `DELETE FROM users` in factory `Initialize` / test ctor, or `EnsureDeleted`+`Migrate` once per class with a GUID database — Testcontainers one container + `Database.EnsureDeleted()` before each class is OK if slow.

Chosen: one container; each WebApplicationFactory appends `SearchingPath` / uses `EF` `EnsureDeleted`+`Migrate` in `ConfigureWebHost` **once** via a static gate per factory type, or truncate Users between tests in existing seed helpers.

- [ ] **Step 4: Run all VSA tests (Docker must be running)**

```bash
dotnet test dotnet/Manager.Vsa.sln
```

Expected: PASS. If Docker missing, fail clearly — document in README: integration tests need Docker.

- [ ] **Step 5: Remove smoke test if redundant; commit**

```bash
git add dotnet/tests/Manager.Vsa.Tests
git commit -m "$(cat <<'EOF'
test: run HTTP integration suites on Testcontainers PostgreSQL

EOF
)"
```

---

### Task 8: Final verification + README note

**Files:**
- Modify: `dotnet/README.md` and/or root `README.md` — Docker required for integration tests; Users admin auth note (one sentence each)
- No product code

- [ ] **Step 1: Run full suites**

```bash
dotnet test dotnet/Manager.Vsa.sln
cd python && uv run pytest -q
```

Expected: all PASS.

- [ ] **Step 2: Grep guards**

```bash
rg "UserNotFoundException|_UserNotFound" dotnet/src python/src
rg "UseInMemoryDatabase" dotnet/tests/Manager.Vsa.Tests --glob '*WebApplicationFactory*'
rg "Raise\\(new UserCreated" -A2 dotnet/src/Manager.Api/Features/Users/CreateUser.cs
```

Expected: no sentinel exceptions in src; no InMemory in WebApplicationFactories; CreateUser still Raises after SaveChanges.

- [ ] **Step 3: Commit docs**

```bash
git add README.md dotnet/README.md python/README.md
git commit -m "$(cat <<'EOF'
docs: note admin Users auth and Testcontainers for integration tests

EOF
)"
```

---

## Self-review

| Spec / gap item | Task |
|-----------------|------|
| Ownership intentional + documented; Authorization folder | Task 1 |
| Domain events Create after Save (documented, not “fixed” to Guid) | Task 1 docs + Task 3 note |
| Cache not-found without sentinel exceptions | Task 2 |
| Testcontainers used for integration | Task 7 |
| Authorization/ present | Task 1 |
| UpdateUser manual validation | Task 4 |
| Python manual validators | Task 5 |
| Email trim .NET parity | Task 6 |
| Login/Refresh domain events | Task 3 |

Placeholder scan: no TBD/TODO left in steps.  
Type consistency: event names `UserLoggedInDomainEvent` / `UserTokenRefreshedDomainEvent` shared across tasks 3 and tests.  
Out of scope (intentional): forcing resource ownership on Users; replacing handler InMemory/SQLite unit tests; building a full permission engine.
