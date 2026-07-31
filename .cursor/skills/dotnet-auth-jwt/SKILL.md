---
name: dotnet-auth-jwt
description: Implementa autenticação JWT e hash Argon2 em ASP.NET Core. Use ao configurar auth, proteger rotas ou quando o usuário menciona JWT, token ou autenticação.
---

# Autenticação JWT neste Projeto

## Componentes

- **TokenService** – `dotnet/src/1 - Manager.API/Token/TokenService.cs` – gera JWT
- **ITokenService** – interface injetada
- **Argon2** – hash de senhas via EscNet, configuração em `AddHash`

## Proteção de Rotas

```csharp
var group = app.MapGroup("/api/v1/users").RequireAuthorization();
```

## Configuração JWT

- `appsettings.json`: `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
- Bearer scheme em `AddJwt`
- Swagger: AddSecurityDefinition("Bearer"), AddSecurityRequirement

## Fluxo Login

1. Endpoint recebe email/senha
2. Busca usuário, valida senha com `_argon2IdHasher.VerifyHashedText(password, hash)`
3. Gera token via `ITokenService.GenerateToken(user)`
4. Retorna token no response
