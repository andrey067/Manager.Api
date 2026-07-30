# Vertical Slice Architecture Migration — Design

**Date:** 2026-07-30  
**Status:** Approved in brainstorming; awaiting user review of this written spec  
**Scope:** `dotnet/` and `python/` Manager API stacks

## Goal

Migrate both stacks from Clean Architecture (layered projects / packages) to **Vertical Slice Architecture (VSA)** aligned with the `vsa-review` skill checklist, keeping feature parity between .NET and Python.

## Decisions

| Topic | Choice |
|-------|--------|
| Fidelity | Full VSA template (single deployable, one file per use case, Result, direct DbContext/Session, no MediatR/repositories) |
| Order | Parallel .NET + Python, feature by feature |
| HTTP contract | Adopt template style (typed success bodies + Problem Details); break current `ResultViewModel` / `ResultSchema` envelope |
| Checklist extras | Domain events on mutations, `IDateTimeProvider` / clock, HybridCache + invalidation |
| Docs | Same effort: AGENTS, cursor rules/skills, READMEs, LikeC4 |
| Execution | Greenfield scaffold + port by slice; delete old layers at cutover |

## Current state (brief)

- **.NET:** five projects (`API`, `Domain`, `Services`, `Infra`, `Core`); `Features/` holds thin Minimal API endpoints that call `IUserService`; MediatR DomainNotifications; repositories; Mapster DTOs.
- **Python:** `domain` → `application` (use cases) → `infrastructure` → `api`; repository ports; DomainNotification handler; Pydantic `ResultSchema` envelope.
- Both expose Users CRUD/search, bootstrap register, Auth login; .NET also has refresh token. Python gains `RefreshToken` during migration for parity.

## Target architecture

### .NET — single project `dotnet/src/Manager.Api`

```
Manager.Api/
  Features/
    Users/
      User.cs, UserErrors.cs, UserCacheKeys.cs, *DomainEvent.cs
      CreateUser.cs, UpdateUser.cs, RemoveUser.cs
      GetUser.cs, GetAllUsers.cs, GetUserByEmail.cs
      SearchUsersByName.cs, SearchUsersByEmail.cs
      RegisterBootstrap.cs
    Auth/
      Login.cs, RefreshToken.cs
  Common/           # Result, Messaging (ICommand/IQuery handlers), CustomResults, IDateTimeProvider
  Database/         # ApplicationDbContext, Configurations/, migrations
  Authentication/   # JWT, IUserContext, password hashing (Argon2)
  Authorization/    # policies if needed
```

- One use case = one file: nested `Command`/`Query`, `Validator` (commands), `Handler`, `Endpoint`, `Response`.
- Handler: `internal sealed`, primary constructor, custom `ICommandHandler<>` / `IQueryHandler<>` — **no MediatR**.
- Handlers inject concrete `ApplicationDbContext` — **no** `IApplicationDbContext` / repository abstractions.
- DI: Scrutor scan handlers; `AddValidatorsFromAssembly(includeInternalTypes: true)`; `AddEndpoints` — no manual per-slice registration.
- Persistence mapping in `IEntityTypeConfiguration<>` under `Database/Configurations/`; entities are plain types under `Features/{Entity}/`.

### Python — idiomatic mirror under `python/src/`

```
features/
  users/   # entity, errors, cache_keys, events + one module per use case
  auth/    # login, refresh_token
common/          # Result, messaging protocols, problem responses, clock
database/        # session/engine, SQLAlchemy models/config, Alembic
authentication/  # JWT, user context, hasher
```

- One use case = one module containing request/command, validator, handler, FastAPI route wiring, and response model.
- Handler uses Session/Db dependency directly (no repository layer).
- Endpoint maps request → command/query, matches `Result` to HTTP (success body or Problem Details).
- Discovery via a single registry/import of feature modules (equivalent of Scrutor/`AddEndpoints`), not hand-wired use-case factories per endpoint as today.

### Removed at cutover

- .NET: `Manager.Domain`, `Manager.Services`, `Manager.Infra`, `Manager.Core`; MediatR DomainNotification pipeline; `IUserRepository` / `BaseRepository`; `ResultViewModel` envelope; Mapster-centric DTO layer for slices (responses live in the slice).
- Python: layered `domain/`, `application/`, `infrastructure/` packages as architecture boundaries; repository interfaces/impls; DomainNotification mediator; generic `ResultSchema` success envelope.

## Slice inventory

| Feature | Slice | Kind | Auth |
|---------|-------|------|------|
| Users | RegisterBootstrap | Command | Anonymous (only when users table empty) |
| Users | CreateUser | Command | JWT |
| Users | UpdateUser | Command | JWT |
| Users | RemoveUser | Command | JWT |
| Users | GetUser | Query | JWT |
| Users | GetAllUsers | Query | JWT |
| Users | GetUserByEmail | Query | JWT |
| Users | SearchUsersByName | Query | JWT |
| Users | SearchUsersByEmail | Query | JWT |
| Auth | Login | Command | Anonymous |
| Auth | RefreshToken | Command | Anonymous (refresh token body) |

Port **the same slice in both stacks** before moving to the next.

## Behavioral conventions

### Command flow

1. Endpoint maps HTTP request → `Command`, dispatches handler.
2. FluentValidation (or Pydantic/Fluent equivalent) validates command shape; handlers do not re-check input shape.
3. Handler enforces business rules; failures return `Result.Failure` with `{Entity}Errors` codes (`Users.EmailConflict`, `Users.NotFound`, `Auth.InvalidCredentials`, etc.) using types `NotFound` / `Conflict` / `Problem` / `Failure`.
4. On mutation: raise domain event on entity (`Raise(...)`) before `SaveChangesAsync` / commit.
5. Invalidate HybridCache keys from `{Feature}CacheKeys` for every command that mutates cached data.
6. Use `IDateTimeProvider` / shared clock — no `DateTime.UtcNow` / `datetime.utcnow` in handlers.
7. Endpoint: `result.Match(success, CustomResults.Problem)` (Python equivalent); no business logic in the endpoint.

### Query flow

- No validator.
- Project to nested `Response` (never return raw domain entities).
- May read through HybridCache; must stay consistent with command-side invalidation.

### Slice isolation

- A feature may use `Common` / `Database` / `Authentication` / `Authorization` and another feature’s **domain types** (entity, `{Entity}Errors`, domain events).
- A feature must **never** reference another feature’s Command/Query/Handler/Validator/Endpoint.

### HTTP contract (breaking)

- Success: typed JSON body from the slice `Response` (or `204 No Content` where appropriate).
- Failure: Problem Details with stable error codes — not `{ message, success, data }`.
- Routes may be reorganized to REST-ish resources if that matches the template style; keep semantic coverage of current operations (create/update/remove/get/search/login/refresh/bootstrap). Exact paths are fixed in the implementation plan; this design requires coverage, not path string equality with `/api/v1/users/create`.

## Testing

For each new/changed slice in both stacks:

- **Handler unit tests:** every `Result.Failure` path + happy path (persisted state + domain events; cache invalidation where applicable).
- **Validator tests:** one assertion set per rule (commands only).
- **Endpoint integration tests:** real HTTP against test host + Postgres (existing fixture pattern adapted to single project).

Layer tests tied to Services/repositories/MediatR are deleted at cutover — no hybrid suite.

Validation gate: `vsa-review` on the change set; `dotnet build` / `dotnet test`; Python `pytest` (and type/lint tools as already configured).

## Documentation and agents

Same migration effort updates:

- Root / `dotnet/AGENTS.md` / `python/AGENTS.md`
- `.cursor/rules/dotnet.mdc` and `python.mdc`
- Skills that teach Clean Architecture layers → VSA (slice creation, messaging, Result); keep / strengthen `vsa-review` as the convention gate
- `docs/architecture` LikeC4 models/views: components become Features + Common + Database (+ Auth), not Domain/Services/Infra
- READMEs: run instructions and API contract notes for Problem Details

## Migration phases (greenfield)

1. **Scaffold** — Create VSA shell in both stacks (`Common`, `Database`, `Authentication`, messaging/`Result`, HybridCache, DateTimeProvider, endpoint discovery). Old code still present but not the long-term entrypoint once scaffold boots.
2. **Auth slices** — `Login`, `RefreshToken` in parallel (.NET + Python) with tests.
3. **Users slices** — `RegisterBootstrap` → commands (Create/Update/Remove) → queries (Get/GetAll/GetByEmail/Search*) one at a time, both stacks.
4. **Cutover** — Solution/`pyproject` and entrypoints point only at VSA; delete old layered projects/packages and obsolete tests.
5. **Docs** — AGENTS, rules, skills, LikeC4, READMEs; final `vsa-review` + full test runs.

## Out of scope

- Frontend or new business domains beyond Users/Auth
- Keeping MediatR, repository abstractions, or the old response envelope “for compatibility”
- HybridCache as a product feature beyond the template pattern (cache is present and wired for the User feature keys the slices define)

## Success criteria

- Both stacks are single-deployable VSA layouts with one file/module per use case.
- No cross-feature slice references; no MediatR; no repository layer for User persistence.
- Mutations raise domain events and invalidate User cache keys; handlers use the clock abstraction.
- HTTP uses typed success + Problem Details with `{Feature}.{Reason}` codes.
- Tests cover handlers/validators/endpoints per conventions above.
- AGENTS, rules, skills, and LikeC4 describe VSA, not Clean Architecture layers.
- `vsa-review` reports no blockers on the migrated code.
`}