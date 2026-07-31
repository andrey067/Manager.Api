# Manager API

Projeto **build to learn**: uma API REST com CRUD de usuários e autenticação JWT, implementada em mais de uma stack para estudo e comparação — com foco em práticas próximas de produção (segredos fora do código, testes, cobertura).

## Objetivo

- Praticar uma API REST com operações CRUD (Create, Read, Update, Delete).
- Implementar autenticação via **JWT** (login contra usuários reais, access + refresh token, rotas protegidas).
- Aplicar **Vertical Slice Architecture (VSA)** em cada stack: um arquivo/módulo por caso de uso.

## O que a API oferece

- **Usuários**: cadastro, listagem, busca, atualização e remoção.
- **Autenticação**: login com e-mail/senha (tabela de usuários), Argon2, JWT + refresh token.
- **Validação** e erros via **Problem Details** com códigos estáveis (`Users.NotFound`, `Auth.InvalidCredentials`, …).
- **Health check**: `GET /health`.

## Contrato HTTP

**Sucesso:** corpo JSON tipado do slice (`Response`) ou `204 No Content` (ex.: delete).

**Falha:** [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807) com código `"{Feature}.{Reason}"` (ex.: `Users.EmailConflict`, `Auth.InvalidRefreshToken`). Não há envelope `{ success, message, data }`.

### Rotas (ambas as stacks)

| Método | Caminho | Slice | Auth |
|--------|---------|-------|------|
| GET | `/health` | health | — |
| POST | `/api/v1/auth/login` | Login | anônimo |
| POST | `/api/v1/auth/refresh` | RefreshToken | anônimo (body com refresh token) |
| POST | `/api/v1/users/bootstrap` | RegisterBootstrap | anônimo (só com tabela vazia) |
| POST | `/api/v1/users` | CreateUser | JWT |
| PUT | `/api/v1/users/{id}` | UpdateUser | JWT |
| DELETE | `/api/v1/users/{id}` | RemoveUser | JWT |
| GET | `/api/v1/users/{id}` | GetUser | JWT |
| GET | `/api/v1/users` | GetAllUsers | JWT |
| GET | `/api/v1/users/by-email?email=` | GetUserByEmail | JWT |
| GET | `/api/v1/users/search-by-name?name=` | SearchUsersByName | JWT |
| GET | `/api/v1/users/search-by-email?email=` | SearchUsersByEmail | JWT |

## Estrutura do repositório

| Pasta | Stack | Onde começar |
|-------|-------|--------------|
| **dotnet/** | .NET (VSA, ASP.NET Core, EF Core) | [dotnet/README.md](dotnet/README.md) |
| **python/** | Python (VSA, FastAPI, SQLAlchemy) | [python/README.md](python/README.md) |

Cada pasta tem README próprio com pré-requisitos, configuração (banco, JWT **via User Secrets / env**), como rodar e testes.

## Como usar (resumo)

1. Entre em `dotnet/` ou `python/` e configure segredos (nunca coloque senhas/JWT em arquivos versionados).
2. Crie o primeiro usuário com `POST /api/v1/users/bootstrap` (só funciona com banco vazio).
3. Faça login em `POST /api/v1/auth/login` e use o Bearer token nas rotas de usuários.
4. Renove tokens com `POST /api/v1/auth/refresh`.

Rotas de usuários exigem JWT, mas qualquer caller autenticado pode gerenciar qualquer usuário (admin CRUD — sem filtro por ownership).

Integração HTTP (.NET) usa Testcontainers PostgreSQL e exige Docker local; ver [dotnet/README.md](dotnet/README.md#tests).

## Licença e referências

Projeto de estudo. Referências e créditos estão nos READMEs de cada stack.

---

© 2026
