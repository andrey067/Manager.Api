---
name: dotnet-service-di
description: Registra serviços, configura JWT, Swagger, Mapster e pipeline em ASP.NET Core. Use ao configurar DI, adicionar novos serviços ou quando o usuário menciona ServiceCollection, AddScoped ou pipeline.
---

# Registro de Serviços e Configuração

## Local

`dotnet/src/1 - Manager.API/Extensions/ServiceCollectionExtensions.cs`

## Padrão de Registro

```csharp
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<ITokenService, TokenService>();
```

## Extensões Usadas

- `AddManagerServices(configuration)` – agrupa tudo
- `AddJwt` – Bearer, TokenValidationParameters
- `AddMapster` – configurações de mapeamento
- `AddDatabase` – DbContext, SQL Server
- `AddSwagger` – OpenAPI, Bearer security
- `AddHash` – Argon2 (EscNet)
- `AddMediator` – MediatR, DomainNotificationHandler

## Pipeline

`dotnet/src/1 - Manager.API/Extensions/ApplicationBuilderExtensions.cs` – `UseManagerPipeline()` (middleware, Swagger, auth).
