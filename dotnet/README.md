# Manager API (.NET)

REST API for user CRUD with JWT authentication, built with **ASP.NET Core Minimal API**, **Vertical Slice Architecture**, and EF Core on PostgreSQL.

## Overview

- User management (create, read, update, delete, search)
- Login against the `User` table (email/password) with **Argon2** verification
- JWT access tokens + rotating refresh tokens (hash stored in DB)
- FluentValidation per command slice; domain events on mutations
- Health check at `GET /health`

## Architecture (VSA)

Single project **`Manager.Api`** (`src/Manager.Api/`):

| Folder | Responsibility |
|--------|----------------|
| `Features/{Entity}/` | One file per use case + entity, `{Entity}Errors`, `{Entity}CacheKeys`, domain events |
| `Common/` | `Result`, messaging handlers, `CustomResults`, `IDateTimeProvider` |
| `Database/` | `ApplicationDbContext`, `Configurations/`, migrations |
| `Authentication/` | JWT, `IUserContext`, Argon2 |

Open **`Manager.Vsa.sln`** — it contains only **`Manager.Api`** and **`Manager.Vsa.Tests`** (layered projects were removed in Task 17 cutover).

## Stack

- .NET 10 / ASP.NET Core Minimal API
- Entity Framework Core 10 + PostgreSQL (Npgsql)
- FluentValidation, Scrutor, HybridCache
- JWT Bearer + Argon2 (EscNet)
- xUnit, FluentAssertions, Coverlet

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 14+ (runtime and migrations)

## Install

```bash
cd dotnet
dotnet restore Manager.Vsa.sln
dotnet build Manager.Vsa.sln
```

## Configuration (no secrets in git)

`appsettings.json` only has **empty placeholders**. Never commit real passwords or JWT keys.

### User Secrets (recommended for local)

```bash
cd src/Manager.Api
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
| `Hash__Salt` | Optional Argon2 salt |

## Run

```bash
cd dotnet
dotnet run --project "src/Manager.Api/Manager.Api.csproj"
```

- Swagger UI: `/swagger`
- Health: `GET /health`

## HTTP contract

**Success:** typed JSON from the slice `Response` (e.g. `Login.Response` with `AccessToken`, `AccessTokenExpires`, `RefreshToken`, `RefreshTokenExpires`) or `204 No Content`.

**Failure:** Problem Details (`application/problem+json`) with extension `code` set to `"{Feature}.{Reason}"`:

| Code | When |
|------|------|
| `Users.NotFound` | User id/email not found |
| `Users.EmailConflict` | Duplicate email on create/update |
| `Validation.Error` | Request/command validation failure |
| `Users.BootstrapNotAllowed` | Bootstrap when users already exist |
| `Auth.InvalidCredentials` | Login email/password mismatch |
| `Auth.InvalidRefreshToken` | Missing, expired, or unknown refresh token |

### Routes

| Method | Path | Auth |
|--------|------|------|
| GET | `/health` | — |
| POST | `/api/v1/auth/login` | anonymous |
| POST | `/api/v1/auth/refresh` | anonymous (refresh token body) |
| POST | `/api/v1/users/bootstrap` | anonymous (empty users table only) |
| POST | `/api/v1/users` | JWT |
| PUT | `/api/v1/users/{id}` | JWT |
| DELETE | `/api/v1/users/{id}` | JWT |
| GET | `/api/v1/users/{id}` | JWT |
| GET | `/api/v1/users` | JWT |
| GET | `/api/v1/users/by-email?email=` | JWT |
| GET | `/api/v1/users/search-by-name?name=` | JWT |
| GET | `/api/v1/users/search-by-email?email=` | JWT |

### Bootstrap (first user)

```http
POST /api/v1/users/bootstrap
{ "name": "Admin", "email": "admin@example.com", "password": "Secret1!" }
```

Returns `400` Problem Details (`Users.BootstrapNotAllowed`) once any user exists.

### Login

```http
POST /api/v1/auth/login
{ "login": "admin@example.com", "password": "Secret1!" }
```

### Refresh

```http
POST /api/v1/auth/refresh
{ "refreshToken": "..." }
```

### Protected routes

Send `Authorization: Bearer {accessToken}` on all `/api/v1/users/*` routes except bootstrap.

**Authorization (Users):** JWT is required, but handlers do not scope rows to `IUserContext.UserId` — any authenticated caller may CRUD any user (admin resource management; see `Authorization/AdminResourceAccess`).

## Migrations

```bash
cd dotnet
dotnet ef database update \
  --project "src/Manager.Api/Manager.Api.csproj" \
  --startup-project "src/Manager.Api/Manager.Api.csproj"
```

## Tests

```bash
cd dotnet
dotnet test Manager.Vsa.sln
```

HTTP integration tests (`*EndpointTests`, `WebApplicationFactory`) use Testcontainers PostgreSQL and require a local Docker API (`unix:///var/run/docker.sock` or `tcp://`). Handler unit tests still use EF Core InMemory.

## Project structure

```
dotnet/
  src/Manager.Api/
    Features/       Users/, Auth/ — one .cs file per slice
    Common/         Result, Messaging, CustomResults
    Database/       ApplicationDbContext, Configurations/
    Authentication/ JWT, IUserContext, Argon2
  tests/Manager.Vsa.Tests/
  Manager.Vsa.sln
```

## Conventions

- One use case = one file; handlers use concrete `ApplicationDbContext` (no repositories)
- `result.Match(success, CustomResults.Problem)` in endpoints
- `IDateTimeProvider` in handlers; domain events + HybridCache invalidation on mutations
- Run `/vsa-review` before committing slice changes

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Startup throws about `Jwt:Key` | Set User Secret / env var (≥ 32 chars) |
| `401` on user routes | Login and send `Authorization: Bearer …` |
| `400` with `Users.BootstrapNotAllowed` on bootstrap | First user already exists — use login + `POST /api/v1/users` |
| Problem Details `Users.EmailConflict` | Choose a different email |

---

© 2026
