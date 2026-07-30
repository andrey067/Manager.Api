# Vertical Slice Architecture Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate `dotnet/` and `python/` Manager API from Clean Architecture layers to Vertical Slice Architecture with feature parity, Result/Problem Details, direct DbContext/Session, domain events, HybridCache, and updated docs/agents.

**Architecture:** Greenfield VSA shells (`dotnet/src/Manager.Api`, `python/src/{features,common,database,authentication}`) run alongside legacy code until cutover. One use case = one file/module. Port each slice in **both stacks before the next slice**. Spec: `docs/superpowers/specs/2026-07-30-vertical-slice-migration-design.md`.

**Tech Stack:** .NET 10, Minimal APIs, EF Core + Npgsql, FluentValidation, Scrutor, HybridCache, JWT + Argon2 (EscNet), xUnit + Testcontainers. Python 3.12+, FastAPI, SQLAlchemy async + asyncpg, Alembic, PyJWT, argon2-cffi, Pydantic, pytest + aiosqlite/Postgres fixtures. uv/poe for Python.

## Global Constraints

- Fidelity: full VSA — single deployable per stack; no MediatR; no repository abstractions; concrete `ApplicationDbContext` / SQLAlchemy `AsyncSession` in handlers.
- Order: same slice in .NET **and** Python before moving on.
- HTTP: typed success body or 204; failures = Problem Details with codes `"{Feature}.{Reason}"` (e.g. `Users.NotFound`, `Auth.InvalidCredentials`).
- No `DateTime.UtcNow` / `datetime.now(UTC)` in handlers — use `IDateTimeProvider` / `Clock`.
- Mutations: `entity.Raise(...)` then save; invalidate `UserCacheKeys` on every User mutation.
- Handlers never return domain entities — project to slice `Response`.
- No cross-feature imports of Command/Query/Handler/Validator/Endpoint.
- TDD: failing test → implement → pass → commit per task.
- Legacy projects under `dotnet/src/1 - Manager.API` … `5 - Manager.Core` and Python `domain`/`application`/`infrastructure`/`api` stay until Task 17 (cutover).

### Locked HTTP routes

| Method | Path | Slice |
|--------|------|-------|
| GET | `/health` | scaffold |
| POST | `/api/v1/auth/login` | Login |
| POST | `/api/v1/auth/refresh` | RefreshToken |
| POST | `/api/v1/users/bootstrap` | RegisterBootstrap |
| POST | `/api/v1/users` | CreateUser |
| PUT | `/api/v1/users/{id:long}` | UpdateUser |
| DELETE | `/api/v1/users/{id:long}` | RemoveUser |
| GET | `/api/v1/users/{id:long}` | GetUser |
| GET | `/api/v1/users` | GetAllUsers |
| GET | `/api/v1/users/by-email?email=` | GetUserByEmail |
| GET | `/api/v1/users/search-by-name?name=` | SearchUsersByName |
| GET | `/api/v1/users/search-by-email?email=` | SearchUsersByEmail |

### Error codes (factories)

- `Users.NotFound`, `Users.EmailConflict`, `Users.Validation`, `Users.BootstrapNotAllowed`
- `Auth.InvalidCredentials`, `Auth.InvalidRefreshToken`

---

## File structure (target)

### .NET — create under `dotnet/`

```
src/Manager.Api/
  Manager.Api.csproj
  Program.cs
  appsettings.json
  appsettings.Development.json
  Common/
    Error.cs, Result.cs, ResultT.cs, ErrorType.cs
    CustomResults.cs, Tags.cs, IEndpoint.cs, EndpointExtensions.cs
    Messaging/ICommand.cs, ICommandHandler.cs, IQuery.cs, IQueryHandler.cs
    Messaging/Dispatcher.cs (or Mediator-less invoker used by endpoints)
    IDateTimeProvider.cs, SystemDateTimeProvider.cs
    Domain/Entity.cs, IDomainEvent.cs
  Database/
    ApplicationDbContext.cs
    Configurations/UserConfiguration.cs
  Authentication/
    IPasswordHasher.cs, Argon2PasswordHasher.cs
    ITokenService.cs, JwtTokenService.cs
    IUserContext.cs, HttpUserContext.cs
    AuthErrors.cs
  Features/Users/
    User.cs, UserErrors.cs, UserCacheKeys.cs
    UserCreatedDomainEvent.cs, UserUpdatedDomainEvent.cs, UserRemovedDomainEvent.cs
    RegisterBootstrap.cs, CreateUser.cs, UpdateUser.cs, RemoveUser.cs
    GetUser.cs, GetAllUsers.cs, GetUserByEmail.cs
    SearchUsersByName.cs, SearchUsersByEmail.cs
  Features/Auth/
    Login.cs, RefreshToken.cs
tests/Manager.Vsa.Tests/
  Manager.Vsa.Tests.csproj
  Common/ResultTests.cs, …
  Features/... (handler, validator, endpoint tests)
  Infrastructure/CustomWebApplicationFactory.cs, PostgresCollection.cs
```

### Python — create under `python/src/` (new packages; old remain until cutover)

```
common/
  __init__.py, error.py, result.py, problem.py, clock.py, cache.py, entity.py
  messaging.py
database/
  __init__.py, session.py, base.py, config.py
  models.py  # SQLAlchemy mapped User table (or map entity)
authentication/
  __init__.py, password_hasher.py, token_service.py, user_context.py, errors.py
features/
  __init__.py
  users/
    __init__.py, entity.py, errors.py, cache_keys.py, events.py
    register_bootstrap.py, create_user.py, update_user.py, remove_user.py
    get_user.py, get_all_users.py, get_user_by_email.py
    search_users_by_name.py, search_users_by_email.py
  auth/
    __init__.py, login.py, refresh_token.py
app/
  __init__.py, main.py, deps.py, registry.py
tests/
  common/, features/, api/  (mirror)
```

Update `pyproject.toml` hatch packages + coverage sources to new packages when scaffolds land; finalize at cutover.

---

### Task 1: .NET Common — Result and Error

**Files:**
- Create: `dotnet/src/Manager.Api/Manager.Api.csproj`
- Create: `dotnet/src/Manager.Api/Common/ErrorType.cs`
- Create: `dotnet/src/Manager.Api/Common/Error.cs`
- Create: `dotnet/src/Manager.Api/Common/Result.cs`
- Create: `dotnet/src/Manager.Api/Common/ResultT.cs`
- Create: `dotnet/tests/Manager.Vsa.Tests/Manager.Vsa.Tests.csproj`
- Create: `dotnet/tests/Manager.Vsa.Tests/Common/ResultTests.cs`
- Modify: `dotnet/Directory.Packages.props` — add Scrutor, HybridCache, FluentValidation.DependencyInjectionExtensions; keep FluentValidation, EF, JWT, EscNet, test packages
- Modify: `dotnet/Manager.sln` — add `Manager.Api` and `Manager.Vsa.Tests` projects (leave legacy projects for now)

**Interfaces:**
- Produces: `ErrorType` enum (`Failure`, `Validation`, `NotFound`, `Conflict`, `Problem`); `Error` record with `Code`, `Description`, `Type`; `Result` / `Result<T>` with `IsSuccess`, `Error`, `Value`, static `Success`/`Failure`, and `Match` methods used by endpoints later.

- [ ] **Step 1: Create projects and add to solution**

```bash
cd dotnet
dotnet new web -n Manager.Api -o "src/Manager.Api" --no-https false
dotnet new xunit -n Manager.Vsa.Tests -o "tests/Manager.Vsa.Tests"
dotnet sln Manager.sln add "src/Manager.Api/Manager.Api.csproj"
dotnet sln Manager.sln add "tests/Manager.Vsa.Tests/Manager.Vsa.Tests.csproj"
dotnet add "tests/Manager.Vsa.Tests/Manager.Vsa.Tests.csproj" reference "src/Manager.Api/Manager.Api.csproj"
```

Edit `Manager.Api.csproj` to `net10.0`, enable nullable, and PackageReferences (versions from CPM): `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `EscNet`, `Scrutor`, `Microsoft.Extensions.Caching.Hybrid`, `Swashbuckle.AspNetCore`.

Add to `Directory.Packages.props`:

```xml
<PackageVersion Include="Scrutor" Version="6.0.1" />
<PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="9.4.0" />
<PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
```

Test project: FluentAssertions, Moq, Microsoft.AspNetCore.Mvc.Testing, Testcontainers.PostgreSql, coverlet, EF InMemory.

- [ ] **Step 2: Write failing Result tests**

```csharp
// tests/Manager.Vsa.Tests/Common/ResultTests.cs
using FluentAssertions;
using Manager.Api.Common;

namespace Manager.Vsa.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccess_And_Match_Calls_OnSuccess()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        var value = result.Match(() => 1, _ => 0);
        value.Should().Be(1);
    }

    [Fact]
    public void Failure_IsFailure_And_Match_Calls_OnFailure()
    {
        var error = Error.NotFound("Users.NotFound", "User not found");
        var result = Result.Failure(error);
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Users.NotFound");
        var value = result.Match(() => 1, e => 0);
        value.Should().Be(0);
    }

    [Fact]
    public void ResultT_Success_Exposes_Value()
    {
        var result = Result.Success(42);
        result.Value.Should().Be(42);
        result.Match(v => v, _ => -1).Should().Be(42);
    }
}
```

- [ ] **Step 3: Run test — expect fail**

```bash
cd dotnet && dotnet test tests/Manager.Vsa.Tests --filter FullyQualifiedName~ResultTests
```

Expected: FAIL (types missing / compile errors).

- [ ] **Step 4: Implement Result/Error**

```csharp
// Common/ErrorType.cs
namespace Manager.Api.Common;
public enum ErrorType { Failure, Validation, NotFound, Conflict, Problem }

// Common/Error.cs
namespace Manager.Api.Common;
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
    public static Error Problem(string code, string description) => new(code, description, ErrorType.Problem);
}

// Common/Result.cs
namespace Manager.Api.Common;
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public static Result Success() => new(true, null!);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Error);
}

// Common/ResultT.cs
namespace Manager.Api.Common;
public class Result<T> : Result
{
    private readonly T? _value;
    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No value on failure");
    public static Result<T> Success(T value) => new(value, true, null!);
    public new static Result<T> Failure(Error error) => new(default, false, error);
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Error);
}
```

- [ ] **Step 5: Run tests — expect pass**

```bash
cd dotnet && dotnet test tests/Manager.Vsa.Tests --filter FullyQualifiedName~ResultTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add dotnet/src/Manager.Api dotnet/tests/Manager.Vsa.Tests dotnet/Directory.Packages.props dotnet/Manager.sln
git commit -m "feat(dotnet): add Manager.Api Result/Error foundation"
```

---

### Task 2: Python Common — Result and Error

**Files:**
- Create: `python/src/common/__init__.py`
- Create: `python/src/common/error.py`
- Create: `python/src/common/result.py`
- Create: `python/tests/common/__init__.py`
- Create: `python/tests/common/test_result.py`
- Modify: `python/pyproject.toml` — add `src/common` to hatch packages, coverage, ruff known-first-party, isort

**Interfaces:**
- Produces: `ErrorType`, `Error`, `Result[T]` / `Result` with `is_success`, `error`, `value`, `success()`, `failure()`, `match(on_success, on_failure)` mirroring .NET semantics.

- [ ] **Step 1: Write failing tests**

```python
# python/tests/common/test_result.py
from common.error import Error, ErrorType
from common.result import Result


def test_success_match():
    result = Result.success()
    assert result.is_success
    assert result.match(lambda: 1, lambda e: 0) == 1


def test_failure_match():
    err = Error.not_found("Users.NotFound", "User not found")
    result = Result.failure(err)
    assert not result.is_success
    assert result.error.code == "Users.NotFound"
    assert result.error.type == ErrorType.NOT_FOUND
    assert result.match(lambda: 1, lambda e: 0) == 0


def test_result_t_value():
    result = Result.success(42)
    assert result.value == 42
    assert result.match(lambda v: v, lambda e: -1) == 42
```

- [ ] **Step 2: Run — expect fail**

```bash
cd python && uv run pytest tests/common/test_result.py -v --no-cov
```

Expected: FAIL (import errors).

- [ ] **Step 3: Implement**

```python
# python/src/common/error.py
from enum import Enum
from dataclasses import dataclass

class ErrorType(Enum):
    FAILURE = "Failure"
    VALIDATION = "Validation"
    NOT_FOUND = "NotFound"
    CONFLICT = "Conflict"
    PROBLEM = "Problem"

@dataclass(frozen=True, slots=True)
class Error:
    code: str
    description: str
    type: ErrorType

    @staticmethod
    def not_found(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.NOT_FOUND)

    @staticmethod
    def conflict(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.CONFLICT)

    @staticmethod
    def validation(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.VALIDATION)

    @staticmethod
    def failure(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.FAILURE)

    @staticmethod
    def problem(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.PROBLEM)

# python/src/common/result.py
from __future__ import annotations
from collections.abc import Callable
from typing import Generic, TypeVar
from common.error import Error

T = TypeVar("T")
R = TypeVar("R")

class Result(Generic[T]):
    def __init__(self, value: T | None, error: Error | None, is_success: bool) -> None:
        self._value = value
        self._error = error
        self.is_success = is_success

    @property
    def is_failure(self) -> bool:
        return not self.is_success

    @property
    def error(self) -> Error:
        if self._error is None:
            raise RuntimeError("No error on success")
        return self._error

    @property
    def value(self) -> T:
        if not self.is_success:
            raise RuntimeError("No value on failure")
        return self._value  # type: ignore[return-value]

    @staticmethod
    def success(value: T | None = None) -> Result[T]:
        return Result(value, None, True)

    @staticmethod
    def failure(error: Error) -> Result[T]:
        return Result(None, error, False)

    def match(self, on_success: Callable[..., R], on_failure: Callable[[Error], R]) -> R:
        if self.is_success:
            if self._value is None:
                return on_success()
            return on_success(self._value)
        return on_failure(self.error)
```

Update `pyproject.toml` hatch `packages` to include `src/common`; coverage `--cov=common`; `known-first-party` add `common`.

- [ ] **Step 4: Run — expect pass**

```bash
cd python && uv run pytest tests/common/test_result.py -v --no-cov
```

- [ ] **Step 5: Commit**

```bash
git add python/src/common python/tests/common python/pyproject.toml
git commit -m "feat(python): add common Result/Error foundation"
```

---

### Task 3: .NET Messaging, IEndpoint, CustomResults, DateTime, Entity base

**Files:**
- Create: `dotnet/src/Manager.Api/Common/Messaging/ICommand.cs`, `ICommandHandler.cs`, `IQuery.cs`, `IQueryHandler.cs`
- Create: `dotnet/src/Manager.Api/Common/IEndpoint.cs`, `EndpointExtensions.cs`, `Tags.cs`, `CustomResults.cs`
- Create: `dotnet/src/Manager.Api/Common/IDateTimeProvider.cs`, `SystemDateTimeProvider.cs`
- Create: `dotnet/src/Manager.Api/Common/Domain/IDomainEvent.cs`, `Entity.cs`
- Create: `dotnet/tests/Manager.Vsa.Tests/Common/CustomResultsTests.cs`
- Create: `dotnet/tests/Manager.Vsa.Tests/Common/EntityTests.cs`

**Interfaces:**
- Produces:
  - `ICommand` / `ICommand<TResponse>`; `ICommandHandler<TCommand>` / `ICommandHandler<TCommand,TResponse>`
  - `IQuery<TResponse>`; `IQueryHandler<TQuery,TResponse>`
  - `IEndpoint { void MapEndpoint(IEndpointRouteBuilder app); }`
  - `EndpointExtensions.AddEndpoints(IServiceCollection, Assembly)` + `MapEndpoints(IEndpointRouteBuilder)`
  - `CustomResults.Problem(Error)` → `IResult` ProblemDetails with `extensions["errorCode"]=error.Code` and status from `ErrorType`
  - `IDateTimeProvider.UtcNow`
  - `Entity` with `Id`, `IReadOnlyCollection<IDomainEvent> DomainEvents`, `Raise(IDomainEvent)`, `ClearDomainEvents()`

- [ ] **Step 1: Write failing tests for CustomResults status mapping and Entity.Raise**

```csharp
// tests/Manager.Vsa.Tests/Common/CustomResultsTests.cs
using FluentAssertions;
using Manager.Api.Common;
using Microsoft.AspNetCore.Http;

namespace Manager.Vsa.Tests.Common;

public class CustomResultsTests
{
    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Problem, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public async Task Problem_Maps_Status_And_ErrorCode(ErrorType type, int status)
    {
        var error = new Error("Users.Test", "detail", type);
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        await CustomResults.Problem(error).ExecuteAsync(httpContext);
        httpContext.Response.StatusCode.Should().Be(status);
    }
}

// tests/Manager.Vsa.Tests/Common/EntityTests.cs
using FluentAssertions;
using Manager.Api.Common.Domain;

namespace Manager.Vsa.Tests.Common;

file sealed record TestEvent(long Id) : IDomainEvent;
file sealed class TestEntity : Entity;

public class EntityTests
{
    [Fact]
    public void Raise_Adds_DomainEvent_Clear_Removes()
    {
        var entity = new TestEntity();
        entity.Raise(new TestEvent(1));
        entity.DomainEvents.Should().ContainSingle();
        entity.ClearDomainEvents();
        entity.DomainEvents.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run — expect fail**

```bash
cd dotnet && dotnet test tests/Manager.Vsa.Tests --filter FullyQualifiedName~CustomResultsTests|FullyQualifiedName~EntityTests
```

- [ ] **Step 3: Implement messaging + CustomResults + Entity + DateTime**

`CustomResults.Problem` mapping:

| ErrorType | Status |
|-----------|--------|
| NotFound | 404 |
| Conflict | 409 |
| Validation | 400 |
| Problem / Failure | 400 / 500 respectively (`Failure`→500, `Problem`→400) |

`ICommandHandler` signatures:

```csharp
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
```

Register in a temporary `Program.cs` only `AddSingleton<IDateTimeProvider, SystemDateTimeProvider>()` — full DI in Task 5.

- [ ] **Step 4: Run — expect pass**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat(dotnet): add messaging, endpoints helpers, Entity, clock"
```

---

### Task 4: Python messaging, problem responses, clock, entity, cache

**Files:**
- Create: `python/src/common/messaging.py`, `problem.py`, `clock.py`, `entity.py`, `cache.py`
- Create: `python/tests/common/test_problem.py`, `test_entity.py`, `test_cache.py`

**Interfaces:**
- Produces:
  - Protocols `CommandHandler`, `QueryHandler` with `async def handle(...) -> Result`
  - `problem_response(error: Error) -> JSONResponse` (RFC7807-ish: `type`, `title`, `status`, `detail`, `errorCode`)
  - `Clock` protocol with `utc_now() -> datetime` (timezone-aware UTC); `SystemClock`
  - `Entity` with `id`, `raise_event`, `domain_events`, `clear_domain_events`
  - `AppCache` with `async def get_or_set(key, factory, ttl)`, `async def remove(key)`, `async def remove_by_prefix(prefix)` — in-memory dict implementation (Python HybridCache stand-in)

- [ ] **Step 1: Write failing tests**

```python
# tests/common/test_problem.py
from common.error import Error
from common.problem import problem_response

def test_problem_not_found_status():
    resp = problem_response(Error.not_found("Users.NotFound", "missing"))
    assert resp.status_code == 404
    assert resp.body  # JSON includes errorCode

# tests/common/test_entity.py
from common.entity import Entity
from dataclasses import dataclass

@dataclass(frozen=True)
class _Evt:
    id: int

def test_raise_and_clear():
    e = Entity()
    e.raise_event(_Evt(1))
    assert len(e.domain_events) == 1
    e.clear_domain_events()
    assert e.domain_events == ()

# tests/common/test_cache.py
import pytest
from common.cache import AppCache

@pytest.mark.asyncio
async def test_get_or_set_and_remove():
    cache = AppCache()
    v1 = await cache.get_or_set("users:1", lambda: {"id": 1}, ttl_seconds=60)
    v2 = await cache.get_or_set("users:1", lambda: {"id": 99}, ttl_seconds=60)
    assert v1 == v2 == {"id": 1}
    await cache.remove("users:1")
    v3 = await cache.get_or_set("users:1", lambda: {"id": 2}, ttl_seconds=60)
    assert v3 == {"id": 2}
```

- [ ] **Step 2: Run — expect fail**

```bash
cd python && uv run pytest tests/common/test_problem.py tests/common/test_entity.py tests/common/test_cache.py -v --no-cov
```

- [ ] **Step 3: Implement `problem.py`, `entity.py`, `clock.py`, `cache.py`, `messaging.py`**

`problem_response` must set JSON keys: `title`, `status`, `detail`, `errorCode` (and optional `type` URI).  
`AppCache.get_or_set(key, factory, ttl_seconds)` — `factory` may be sync or async callable returning the value.  
`Clock.utc_now()` returns timezone-aware UTC `datetime`.

- [ ] **Step 4: Run — expect pass; commit**

```bash
git add python/src/common python/tests/common
git commit -m "feat(python): add problem responses, entity, clock, AppCache"
```

---

### Task 5: Database + User entity shell (both stacks) + health host

**Files (.NET):**
- Create: `Features/Users/User.cs` (plain entity inheriting `Entity`; properties Name, Email, Password, RefreshTokenHash, RefreshTokenExpiresAt; factory/methods Set*; **no** FluentValidation on entity — validation moves to command Validators)
- Create: `Database/ApplicationDbContext.cs` — `DbSet<User> Users`
- Create: `Database/Configurations/UserConfiguration.cs` — table `User`, unique email, same column shapes as current `UserMap`
- Create: `Program.cs` — register DbContext, HybridCache, DateTimeProvider, `AddValidatorsFromAssembly`, Scrutor scan for `ICommandHandler`/`IQueryHandler`/`IEndpoint`, MapEndpoints, JWT placeholder, `MapGet("/health", () => Results.Ok(new { status = "ok" }))`
- Create: migration under `Database/Migrations/` (or `dotnet ef migrations add InitialVsa`)
- Create: `tests/.../CustomWebApplicationFactory.cs` using Testcontainers Postgres (adapt from `Manager.API.Tests`)
- Test: `GET /health` returns 200

**Files (Python):**
- Create: `features/users/entity.py`, `database/session.py`, `database/models.py` (SQLAlchemy model matching User columns), `app/main.py` with `/health`
- Create: Alembic revision if schema differs; otherwise reuse existing User table shape
- Test: httpx ASGI `/health` → 200

**Interfaces:**
- Produces: runnable hosts with `/health`; `User` persistable; `ApplicationDbContext` / async session factory injectable.

- [ ] **Step 1: Write failing health integration tests (both)**

- [ ] **Step 2: Implement User + DbContext/Session + Program/main + DI registrations**

Scrutor example:

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
// repeat for ICommandHandler<,>, IQueryHandler<,>, IEndpoint
```

HybridCache:

```csharp
services.AddHybridCache();
```

- [ ] **Step 3: Run health tests both stacks — pass**

```bash
cd dotnet && dotnet test tests/Manager.Vsa.Tests --filter FullyQualifiedName~Health
cd python && uv run pytest tests/api/test_health_vsa.py -v --no-cov
```

- [ ] **Step 4: Commit**

```bash
git commit -m "feat: VSA hosts with health, User persistence shell"
```

---

### Task 6: Authentication infrastructure (JWT, hasher, user context, AuthErrors)

**Files (.NET):**
- `Authentication/IPasswordHasher.cs`, `Argon2PasswordHasher.cs` (wrap EscNet)
- `Authentication/ITokenService.cs`, `JwtTokenService.cs` — access + refresh token generate/hash/validate; use `IDateTimeProvider`
- `Authentication/IUserContext.cs`, `HttpUserContext.cs` — `long? UserId`
- `Authentication/AuthErrors.cs` — `InvalidCredentials`, `InvalidRefreshToken`
- Wire JWT bearer in `Program.cs` from config keys (reuse secret names from old `appsettings`)

**Files (Python):**
- Mirror modules under `authentication/`; `AuthErrors` in `authentication/errors.py`

**Interfaces:**
- `IPasswordHasher.Hash(string) / Verify(string password, string hash)`
- `ITokenService.CreateAccessToken(userId, email)`, `CreateRefreshToken()`, `HashRefreshToken(raw)`, `GetAccessExpiry()`, `GetRefreshExpiry()`, `ValidateAccessToken` as needed
- `IUserContext.UserId`

- [ ] **Step 1: Unit tests** — hash verify roundtrip; refresh hash stable; AuthErrors codes exact strings `Auth.InvalidCredentials`, `Auth.InvalidRefreshToken`

- [ ] **Step 2: Implement both stacks**

- [ ] **Step 3: Pass + commit**

```bash
git commit -m "feat: VSA authentication JWT, Argon2, user context"
```

---

### Task 7: Users shared types — Errors, CacheKeys, DomainEvents (both)

**Files:**
- .NET: `UserErrors.cs`, `UserCacheKeys.cs`, `UserCreatedDomainEvent.cs`, `UserUpdatedDomainEvent.cs`, `UserRemovedDomainEvent.cs`
- Python: `features/users/errors.py`, `cache_keys.py`, `events.py`

**Interfaces:**
- `UserErrors.NotFound()` → `Users.NotFound`
- `UserErrors.EmailConflict()` → `Users.EmailConflict`
- `UserErrors.BootstrapNotAllowed()` → `Users.BootstrapNotAllowed`
- `UserCacheKeys.All` = `"users:all"`; `ById(long id)` = $"users:{id}"; `ByEmail(string email)` = $"users:email:{email.ToLowerInvariant()}"

- [ ] **Step 1: Tests asserting exact error codes and cache key formats**

- [ ] **Step 2: Implement**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: UserErrors, UserCacheKeys, user domain events"
```

---

### Task 8: Login slice (both stacks)

**Files:**
- .NET: `Features/Auth/Login.cs` — nested `Command(string Login, string Password)`, `Validator`, `Response(string AccessToken, DateTime AccessTokenExpires, string RefreshToken, DateTime RefreshTokenExpires)`, `Handler`, `Endpoint`
- Python: `features/auth/login.py` — same shapes; register route in `app/registry.py`
- Tests: handler invalid credentials; handler success persists refresh hash; validator empty fields; endpoint POST `/api/v1/auth/login` integration

**Interfaces:**
- Consumes: `ApplicationDbContext`/`AsyncSession`, `IPasswordHasher`, `ITokenService`, `IDateTimeProvider`, `AuthErrors`
- Handler: find user by email/login; verify password; on fail `AuthErrors.InvalidCredentials` (do not reveal which failed); on success set refresh token via clock+token service; return `Response`
- Endpoint: anonymous; `result.Match(Results.Ok, CustomResults.Problem)`

- [ ] **Step 1: Write failing handler + validator + endpoint tests (.NET then Python)**

- [ ] **Step 2: Implement Login.cs and login.py**

Minimal .NET Handler sketch:

```csharp
public sealed class Login
{
    public sealed record Command(string Login, string Password) : ICommand<Response>;
    public sealed record Response(string AccessToken, DateTime AccessTokenExpires, string RefreshToken, DateTime RefreshTokenExpires);
    public sealed class Validator : AbstractValidator<Command> { /* NotEmpty Login/Password */ }
    internal sealed class Handler(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command command, CancellationToken ct)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == command.Login, ct);
            if (user is null || !hasher.Verify(command.Password, user.Password))
                return Result.Failure<Response>(AuthErrors.InvalidCredentials());
            var access = tokens.CreateAccessToken(user.Id, user.Email);
            var refresh = tokens.CreateRefreshToken();
            user.SetRefreshToken(tokens.HashRefreshToken(refresh), tokens.GetRefreshExpiry(clock.UtcNow));
            await db.SaveChangesAsync(ct);
            return Result.Success(new Response(access.Token, access.Expires, refresh, tokens.GetRefreshExpiry(clock.UtcNow)));
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app) =>
            app.MapPost("/api/v1/auth/login", async (Command cmd, ICommandHandler<Command, Response> handler, CancellationToken ct) =>
            {
                var result = await handler.Handle(cmd, ct);
                return result.Match(Results.Ok, CustomResults.Problem);
            }).AllowAnonymous().WithTags(Tags.Auth);
    }
}
```

Python: same behavior; Pydantic model for body; `APIRouter` included by registry.

- [ ] **Step 3: Run tests both — pass**

- [ ] **Step 4: Commit**

```bash
git commit -m "feat: VSA Login slice (.NET + Python)"
```

---

### Task 9: RefreshToken slice (both stacks)

**Files:**
- .NET: `Features/Auth/RefreshToken.cs`
- Python: `features/auth/refresh_token.py` (**new capability** on Python for parity)
- Tests: invalid/expired token → `Auth.InvalidRefreshToken`; success rotates refresh hash

**Interfaces:**
- `Command(string RefreshToken)`; `Response` same shape as Login tokens
- Handler: hash incoming token; find user where hash matches and `RefreshTokenExpiresAt > clock.UtcNow`; else `AuthErrors.InvalidRefreshToken()`; rotate tokens

- [ ] **Step 1: Failing tests both stacks**

- [ ] **Step 2: Implement**

- [ ] **Step 3: Pass + commit**

```bash
git commit -m "feat: VSA RefreshToken slice (.NET + Python)"
```

---

### Task 10: RegisterBootstrap slice (both)

**Files:**
- `RegisterBootstrap.cs` / `register_bootstrap.py`
- Route: `POST /api/v1/users/bootstrap` anonymous

**Interfaces:**
- `Command(string Name, string Email, string Password)` + Validator (name/email/password rules: name 2–80, email valid max 180, password 8–30)
- If `db.Users.AnyAsync()` → `UserErrors.BootstrapNotAllowed()`
- Else create user, hash password, `Raise(new UserCreatedDomainEvent(user.Id))`, SaveChanges, invalidate `UserCacheKeys.All`, return `Response(long Id, string Name, string Email)`

- [ ] **Step 1: Tests** — not allowed when users exist; success when empty; validator rules

- [ ] **Step 2: Implement both**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: VSA RegisterBootstrap slice"
```

---

### Task 11: CreateUser slice (both)

**Files:**
- `CreateUser.cs` / `create_user.py`
- Route: `POST /api/v1/users` + `RequireAuthorization`

**Interfaces:**
- Same command shape as bootstrap; on duplicate email → `UserErrors.EmailConflict()`; raise `UserCreatedDomainEvent`; invalidate `UserCacheKeys.All` and email key; `Response(Id, Name, Email)`

- [ ] **Step 1: Handler tests** (conflict, success+event+cache remove), validator tests, endpoint 401 without JWT + 201/200 with JWT

- [ ] **Step 2: Implement**

Endpoint must inject handler only — map body→command; `RequireAuthorization()`.

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: VSA CreateUser slice"
```

---

### Task 12: UpdateUser slice (both)

**Files:**
- `UpdateUser.cs` / `update_user.py`
- Route: `PUT /api/v1/users/{id}`

**Interfaces:**
- `Command(long Id, string Name, string Email, string Password)`
- Not found → `Users.NotFound`; email taken by other user → `Users.EmailConflict`
- Raise `UserUpdatedDomainEvent`; invalidate `ById`, `ByEmail` (old+new), `All`

- [ ] **Step 1: Tests (all failure paths + happy path)**

- [ ] **Step 2: Implement both**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: VSA UpdateUser slice"
```

---

### Task 13: RemoveUser slice (both)

**Files:**
- `RemoveUser.cs` / `remove_user.py`
- Route: `DELETE /api/v1/users/{id}` → `204` on success

**Interfaces:**
- `Command(long Id)` → `Result` (non-generic)
- Not found → `Users.NotFound`; raise `UserRemovedDomainEvent`; invalidate keys; endpoint `Results.NoContent`

- [ ] **Step 1: Tests**

- [ ] **Step 2: Implement**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: VSA RemoveUser slice"
```

---

### Task 14: GetUser + GetAllUsers queries (both)

**Files:**
- `GetUser.cs`, `GetAllUsers.cs` / `get_user.py`, `get_all_users.py`

**Interfaces:**
- `GetUser.Query(long Id)` → `Response(Id, Name, Email)`; use HybridCache/`AppCache` with `UserCacheKeys.ById`; miss → DB projection; not found → `Users.NotFound`
- `GetAllUsers.Query` → `List<Response>`; cache key `UserCacheKeys.All`
- No validators; endpoints authorized

- [ ] **Step 1: Tests** — not found; cache hit returns same; after CreateUser invalidation list refreshes (integration)

- [ ] **Step 2: Implement**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: VSA GetUser and GetAllUsers queries"
```

---

### Task 15: GetUserByEmail + Search queries (both)

**Files:**
- `GetUserByEmail.cs`, `SearchUsersByName.cs`, `SearchUsersByEmail.cs`
- Python mirrors

**Interfaces:**
- `GetUserByEmail.Query(string Email)` — cache `ByEmail`; 404 if missing
- `SearchUsersByName.Query(string Name)` — `WHERE Name.Contains` (case-insensitive); return list (no cache required unless cheap — if cached, document key in `UserCacheKeys.SearchByName(name)`)
- `SearchUsersByEmail.Query(string Email)` — contains search; optional cache key `SearchByEmail`

For YAGNI on search cache: **do not cache search results** in v1; only All/ById/ByEmail are cached. Still invalidate those on mutations (already done).

- [ ] **Step 1: Tests for each query**

- [ ] **Step 2: Implement all three both stacks**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: VSA GetUserByEmail and search queries"
```

---

### Task 16: Full suite green on VSA hosts + vsa-review gate

**Files:**
- Ensure all slice tests pass; add any missing endpoint integration coverage listed in spec
- Do **not** delete legacy yet

- [ ] **Step 1: Run**

```bash
cd dotnet && dotnet test tests/Manager.Vsa.Tests
cd python && uv run pytest tests/common tests/features tests/api/test_*vsa* -v --no-cov
```

Expected: all PASS.

- [ ] **Step 2: Run `/vsa-review`** (skill) against `dotnet/src/Manager.Api` and `python/src/features` — fix any blockers before cutover

- [ ] **Step 3: Commit fixes if any**

```bash
git commit -m "test: VSA suite green and convention fixes"
```

---

### Task 17: Cutover — remove legacy layered code

**Files:**
- Modify: `dotnet/Manager.sln` — remove old API/Domain/Services/Infra/Core and their test projects; keep `Manager.Api` + `Manager.Vsa.Tests` (and IntegrationBase/Fixtures only if still referenced — otherwise delete or slim to Api.Tests only)
- Delete: `dotnet/src/1 - Manager.API` through `5 - Manager.Core`, old `dotnet/tests/Manager.*.Tests` that target layers
- Delete: `python/src/domain`, `application`, `infrastructure`, `api`, `shared` (after moving any still-needed config into new packages)
- Modify: `python/pyproject.toml` — packages/coverage only `common`, `database`, `authentication`, `features`, `app`
- Modify: entry scripts / README run commands to `Manager.Api` and `uvicorn app.main:app`
- Modify: `.vscode/launch.json` if present to new project paths

- [ ] **Step 1: Switch solution/pyproject entrypoints; delete legacy trees**

- [ ] **Step 2: Full test run**

```bash
cd dotnet && dotnet test
cd python && uv run pytest
```

Expected: PASS; no references to MediatR/DomainNotification/IUserRepository.

- [ ] **Step 3: Commit**

```bash
git commit -m "refactor: cut over to VSA; remove Clean Architecture layers"
```

---

### Task 18: Docs, agents, skills, LikeC4

**Files:**
- Modify: `AGENTS.md`, `dotnet/AGENTS.md`, `python/AGENTS.md`
- Modify: `.cursor/rules/dotnet.mdc`, `.cursor/rules/python.mdc`
- Modify/replace: `.cursor/skills/dotnet-clean-architecture/SKILL.md` → VSA guidance (or add `dotnet-vertical-slice/SKILL.md` and point AGENTS to it); update `python-separation-concerns` accordingly
- Keep/enhance: `.cursor/skills/vsa-review/SKILL.md` paths for this repo (`Manager.Api.Features` not `Web.Api`)
- Modify: `docs/architecture/model-dotnet.c4`, `model-python.c4`, `views-structure.c4`, `views-flows.c4`, `README.md`
- Modify: `dotnet/README.md`, `python/README.md`, root `README.md` — Problem Details contract + routes table

- [ ] **Step 1: Rewrite AGENTS/rules to VSA checklist (slices, Result, no MediatR, DbContext direct)**

- [ ] **Step 2: Update LikeC4 components** from Domain/Services/Infra to Features/Common/Database/Authentication; validate:

```bash
npx likec4@1.59.2 validate --json --no-layout docs/architecture
```

- [ ] **Step 3: Commit**

```bash
git commit -m "docs: align AGENTS, skills, and LikeC4 with VSA"
```

---

## Self-review (plan vs spec)

| Spec requirement | Task(s) |
|------------------|---------|
| Single deployable VSA both stacks | 1–5, 17 |
| One file/module per use case | 8–15 |
| No MediatR / no repositories | 3, 5, 17 |
| Result + Problem Details + error codes | 1–4, 8–15 |
| Domain events + DateTimeProvider + HybridCache/AppCache | 3–7, 10–15 |
| Parallel slice port | Tasks 8–15 each say both stacks |
| Auth Login + RefreshToken (Python gains refresh) | 8–9 |
| All User slices including bootstrap | 10–15 |
| Tests handlers/validators/endpoints | per-slice tasks + 16 |
| Docs/skills/LikeC4 | 18 |
| Cutover delete layers | 17 |
| Locked routes | Global Constraints table |

**Placeholder scan:** cleaned — Tasks 3–4 include full test bodies; later slice tasks include handler/endpoint contracts and required test categories (every `Result.Failure` path + happy path + validator + HTTP).

**Type consistency:** `Result`/`Error`/`UserErrors`/`AuthErrors`/`UserCacheKeys`/`IDateTimeProvider`/`Clock`/`AppCache` names are stable across tasks; Login/Refresh `Response` token field names: `AccessToken`, `AccessTokenExpires`, `RefreshToken`, `RefreshTokenExpires`.

---

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-07-30-vertical-slice-migration.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks  
2. **Inline Execution** — execute in this session with executing-plans and checkpoints  

Which approach?
