---
name: dotnet-entity-framework
description: Configura Entity Framework Core, DbContext, migrations e repositórios genéricos em .NET. Use ao criar entidades, migrations, repositórios ou quando o usuário menciona EF Core, DbContext ou migrations.
---

# Entity Framework Core neste Projeto

## DbContext

- `dotnet/src/4 - Manager.Infra/Context/ManagerContext.cs`
- `DbSet<T>` por entidade
- Configurações via Fluent API em `Mappings/`

## Repositório Base

```csharp
// Interface: IBaseRepository<T>
Task<T?> GetAsync(long id);
Task<T?> GetAsync(Expression<Func<T, bool>> filter, bool asNoTracking = true);
Task<IList<T>> GetAllAsync();
Task<IList<T>> SearchAsync(Expression<Func<T, bool>> filter, bool asNoTracking = true);
Task<T> CreateAsync(T entity);
Task<T> UpdateAsync(T entity);
Task RemoveAsync(long id);
```

```csharp
// Repositório específico herda BaseRepository<T>
public class UserRepository : BaseRepository<User>, IUserRepository { }
```

## Nova Migration

```bash
cd dotnet
dotnet ef migrations add NomeDaMigration -p src/4\ -\ Manager.Infra -s src/1\ -\ Manager.API
```

## Mapping Fluent API

Exemplo em `Mappings/UserMap.cs`: `entity.Property()`, `entity.HasKey()`, etc.
