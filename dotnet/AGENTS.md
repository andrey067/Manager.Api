# Agente Dotnet

Instruções para desenvolvimento .NET neste projeto. Aplica-se ao trabalhar em `dotnet/` ou arquivos `.cs`.

## Arquitetura (VSA)

Projeto único **`Manager.Api`** (`dotnet/src/Manager.Api/`):

| Pasta | Responsabilidade |
|-------|------------------|
| `Features/{Entity}/` | Um arquivo por slice (Command/Query, Validator, Handler, Endpoint, Response) + entidade, `{Entity}Errors`, `{Entity}CacheKeys`, domain events |
| `Common/` | `Result`, messaging (`ICommandHandler`/`IQueryHandler`), `CustomResults`, `IDateTimeProvider` |
| `Database/` | `ApplicationDbContext`, `Configurations/`, migrations |
| `Authentication/` | JWT, `IUserContext`, hash Argon2 |
| `Authorization/` | Helpers de política de acesso (ex.: `AdminResourceAccess`) |

Solução: **`Manager.Vsa.sln`** — apenas **`Manager.Api`** (`src/Manager.Api/`) e **`Manager.Vsa.Tests`** (`tests/Manager.Vsa.Tests/`). O cutover (Task 17) removeu os projetos em camadas legados.

## Checklist obrigatório

1. **Slice**: um caso de uso = um arquivo em `Features/{Entity}/{UseCase}.cs`; tipos aninhados (`Command`, `Validator`, `Handler`, `Endpoint`, `Response`).
2. **Handler**: `internal sealed`, primary constructor, `ICommandHandler<>` / `IQueryHandler<>` custom — **sem MediatR**.
3. **Dados**: handler injeta `ApplicationDbContext` concreto — **sem** repositórios nem `IApplicationDbContext`.
4. **Erros**: `Result` / `Result<T>`; códigos `{Feature}.{Reason}` via `{Entity}Errors`; endpoint usa `result.Match(..., CustomResults.Problem)`.
5. **Validação**: FluentValidation no `Validator` do comando; queries sem validator.
6. **Mutações**: `entity.Raise(...)` antes de `SaveChangesAsync`; invalidar `HybridCache` com `{Entity}CacheKeys`.
7. **Tempo**: `IDateTimeProvider` — nunca `DateTime.UtcNow` em handlers.
8. **Isolamento**: não importar Command/Query/Handler/Validator/Endpoint de outra feature; só tipos de domínio compartilhados (entidade, errors, events).
9. **DI**: Scrutor + `AddValidatorsFromAssembly(includeInternalTypes: true)` + `AddEndpoints` — sem registro manual por slice.

## Segurança — Users (admin CRUD)

A feature **Users** é gerenciamento administrativo de recursos: endpoints exigem JWT (`.RequireAuthorization()`), mas handlers **não** filtram linhas por `IUserContext.UserId`. `IUserContext` existe para recursos futuros com ownership e para claims; hoje não escopa linhas de User. Ver `Authorization/AdminResourceAccess`.

## Skills .NET

Invocar conforme a tarefa:

- `/dotnet-vertical-slice` – novos slices, estrutura VSA
- `/dotnet-minimal-api-endpoints` – endpoints Minimal API
- `/dotnet-entity-framework` – DbContext, configurations, migrations
- `/dotnet-service-di` – registro de serviços, Scrutor
- `/dotnet-unit-testing` – testes xUnit (handler, validator, endpoint)
- `/dotnet-auth-jwt` – JWT, refresh token
- `/vsa-review` – auditoria de convenções antes do commit
