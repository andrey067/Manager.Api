# Manager API (.NET)

REST API for user CRUD with JWT authentication, built with **ASP.NET Core Minimal API**, Clean Architecture, and EF Core on PostgreSQL.

## Overview

- User management (create, read, update, delete, search)
- Login against the `User` table (email/password) with **Argon2** verification
- JWT access tokens + rotating refresh tokens (hash stored in DB)
- Domain validation via FluentValidation + MediatR notifications
- Health check at `/health`

## Architecture

| Layer | Project | Responsibility |
|-------|---------|----------------|
| API | `1 - Manager.API` | Minimal API endpoints (`Features/`), JWT, Swagger, DI |
| Domain | `2 - Manager.Domain` | Entities, FluentValidation |
| Services | `3 - Manager.Services` | Application services / use cases |
| Infra | `4 - Manager.Infra` | EF Core, repositories, migrations |
| Core | `5 - Manager.Core` | MediatR notifications, `Optional<T>`, shared messages |

Vertical slices live under `Features/Auth` and `Features/Users`.

## Stack

- .NET 10 / ASP.NET Core Minimal API
- Entity Framework Core 10 + PostgreSQL (Npgsql)
- MediatR, Mapster, FluentValidation
- JWT Bearer + Argon2 (EscNet / Isopoh)
- xUnit, Moq, FluentAssertions, Testcontainers, Coverlet

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 14+ (or Docker for Testcontainers during tests)
- Docker (for integration tests)

## Install

```bash
cd dotnet
dotnet restore Manager.sln
dotnet build Manager.sln
```

## Configuration (no secrets in git)

`appsettings.json` only has **empty placeholders**. Never commit real passwords or JWT keys.

### User Secrets (recommended for local)

```bash
cd "src/1 - Manager.API"
dotnet user-secrets set "ConnectionStrings:ManagerAPIPostgres" "Host=localhost;Port=5432;Database=manager_api;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS"
dotnet user-secrets set "Jwt:Issuer" "Manager.API"
dotnet user-secrets set "Jwt:Audience" "Manager.API"
dotnet user-secrets set "Jwt:HoursToExpire" "1"
dotnet user-secrets set "Jwt:RefreshDaysToExpire" "7"
dotnet user-secrets set "Hash:Salt" "REPLACE_WITH_RANDOM_SALT"
```

### Environment variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__ManagerAPIPostgres` | PostgreSQL connection string |
| `Jwt__Key` | HMAC signing key (**min 32 characters**) |
| `Jwt__Issuer` | Optional JWT issuer |
| `Jwt__Audience` | Optional JWT audience |
| `Jwt__HoursToExpire` | Access token lifetime in hours (default `1`) |
| `Jwt__RefreshDaysToExpire` | Refresh token lifetime in days (default `7`) |
| `Hash__Salt` | Optional Argon2 salt; if empty, a secure random salt is generated at startup |
| `Hash__TimeCost` / `Hash__MemoryCost` / `Hash__Lanes` / `Hash__HashLength` | Argon2 tuning |

Production can also use Azure Key Vault (`AzureKeyVault:Vault`, `ClientId`, `ClientSecret`) when those values are present.

## Run

```bash
cd dotnet
dotnet run --project "src/Manager.Api/Manager.Api.csproj"
```

- Swagger UI: `/swagger`
- Health: `GET /health`

## Authentication

### Bootstrap (first user)

There is a chicken-and-egg problem: CRUD requires JWT, but you need a user to login.

**Solution:** `POST /api/v1/users/register` is **anonymous** and succeeds **only when the users table is empty**. After the first user exists, it returns `403`.

```http
POST /api/v1/users/register
{ "name": "Admin", "email": "admin@example.com", "password": "Secret1!" }
```

Then login and use Bearer tokens for all `/api/v1/users/*` routes (except register).

### Login

```http
POST /api/v1/auth/login
{ "email": "admin@example.com", "password": "Secret1!" }
```

Response includes `token` (JWT), `refreshToken`, and expiry timestamps. Password checks use Argon2 **Verify** against the stored hash (not static config credentials).

### Refresh

```http
POST /api/v1/auth/refresh
{ "refreshToken": "..." }
```

Issues a new access token and rotates the refresh token (SHA-256 hash stored on `User`).

### Protected routes

All `/api/v1/users/*` endpoints except `/register` use `.RequireAuthorization()`. Send:

```http
Authorization: Bearer {access_token}
```

## Migrations

```bash
cd dotnet
dotnet ef database update \
  --project "src/4 - Manager.Infra/Manager.Infra.csproj" \
  --startup-project "src/1 - Manager.API/Manager.API.csproj"
```

Create a new migration:

```bash
dotnet ef migrations add MigrationName \
  --project "src/4 - Manager.Infra/Manager.Infra.csproj" \
  --startup-project "src/1 - Manager.API/Manager.API.csproj" \
  --output-dir Migrations
```

Email has a **unique index** (`IX_User_Email`).

## Tests & coverage

```bash
cd dotnet
# Preferred: sequential merge so Coverlet aggregates across projects
bash scripts/test-with-coverage.sh
```

Or:

```bash
dotnet test Manager.sln /p:CollectCoverage=true
```

Coverage is merged across test projects. **Manager.API.Tests** enforces a **90%** threshold on the merged total for **line, branch, and method** (fails the build if below).

Integration tests prefer **Testcontainers PostgreSQL** when Docker is available; otherwise they fall back to **EF InMemory** so the suite still runs.

## Project structure

```
dotnet/
  src/
    1 - Manager.API/          Features/, Token/, Extensions/, ViewModels/
    2 - Manager.Domain/       Entities/, Validators/
    3 - Manager.Services/     Services/, DTO/, Interfaces/
    4 - Manager.Infra/        Context/, Repositories/, Mappings/, Migrations/
    5 - Manager.Core/         Communication/, Structs/, Enum/
  tests/
    Manager.*.Tests/
    Manager.Fixtures/
    Manager.IntegrationBase/
```

## Conventions

- Prefer interfaces (`IUserService`, `ITokenService`, `IUserRepository`) in endpoints and services
- Mapster for ViewModel ↔ DTO ↔ Entity
- Domain notifications via MediatR; same scoped `DomainNotificationHandler` instance for handlers and endpoints
- DataAnnotations validation filter on Minimal API endpoints
- `DbContext` lifetime: **Scoped** (never Transient)
- No secrets in source control (see `.gitignore`)

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Startup throws about `Jwt:Key` | Set User Secret / env var (≥ 32 chars) |
| Startup throws about connection string | Configure `ConnectionStrings:ManagerAPIPostgres` |
| `401` on user routes | Login and send `Authorization: Bearer …` |
| `403` on `/users/register` | First user already exists — use login + `/users/create` |
| Integration tests fail | Ensure Docker is running (Testcontainers) |
| Coverage below 90% | Run full `dotnet test` with CollectCoverage and add tests for uncovered paths |

## References

Originally based on [Lucas Eschechola’s series](https://www.youtube.com/playlist?list=PLdhhExru1TXcTTm-Mpfg2tN5B_rOTNvzy), updated for .NET 10, Minimal API, and production-readiness hardening.

---

© 2026
