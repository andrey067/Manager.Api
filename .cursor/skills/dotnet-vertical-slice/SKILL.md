---
name: dotnet-vertical-slice
description: Aplica Vertical Slice Architecture em .NET no Manager.Api — um arquivo por caso de uso, Result, DbContext direto, sem MediatR. Use ao criar slices, features ou quando o usuário menciona VSA, vertical slice ou casos de uso.
---

# Vertical Slice Architecture (.NET)

## Estrutura

```
dotnet/src/Manager.Api/
  Features/
    Users/     User.cs, UserErrors.cs, UserCacheKeys.cs, *DomainEvent.cs
               CreateUser.cs, UpdateUser.cs, ...
    Auth/      Login.cs, RefreshToken.cs
  Common/      Result, Messaging, CustomResults, IDateTimeProvider
  Database/    ApplicationDbContext, Configurations/
  Authentication/  JWT, IUserContext, Argon2
```

## Novo slice

1. **Arquivo único** `Features/{Entity}/{UseCase}.cs` com `public static class {UseCase}` e tipos aninhados:
   - Comando: `Command`, `Validator`, `Handler`, `Endpoint`, `Response`
   - Query: `Query`, `Handler`, `Endpoint`, `Response` (sem Validator)
2. **Handler** `internal sealed` com primary constructor; implementa `ICommandHandler<Command, Response>` ou `IQueryHandler<Query, Response>`.
3. **Persistência**: injetar `ApplicationDbContext`; mapeamento em `Database/Configurations/{Entity}Configuration.cs`.
4. **Erros**: factories em `{Entity}Errors` com códigos `"{Feature}.{Reason}"` e tipo semântico (`NotFound`, `Conflict`, `Problem`, `Failure`).
5. **Endpoint** (`IEndpoint`): mapear request → command/query; `result.Match(Results.Ok|NoContent, CustomResults.Problem)`; `.RequireAuthorization()` quando aplicável.
6. **Mutação**: `entity.Raise(new XDomainEvent(...))` antes de `SaveChangesAsync`; `cache.RemoveAsync` com chaves de `{Entity}CacheKeys`.
7. **Testes**: handler (cada `Failure` + happy path), validator (`TestValidate` por regra), endpoint (HTTP integração).

## Proibido

- MediatR, `IUserRepository`, `IApplicationDbContext`, Mapster entre camadas
- Referenciar Command/Handler/Endpoint de outra feature
- `DateTime.UtcNow` em handlers
- Registro manual de handler/validator/endpoint (usar Scrutor + `AddEndpoints`)

## Revisão

Rodar `/vsa-review` no diff antes de commitar.
