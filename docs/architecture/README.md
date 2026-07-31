# Architecture (LikeC4)

C4 diagrams for the Manager API learning project — **Vertical Slice Architecture** (Features + Common + Database + Authentication) with User table login, Argon2 verify, JWT, and Problem Details error responses.

There is **no frontend** in this repository. Consumers are an **API Client** (curl, Postman, tests, other services) and a **Developer** exploring either stack.

## Prerequisites

- Node.js 18+ (for `npx`)
- LikeC4 **≥ 1.59** (dynamic `alt` / `when` flow blocks)

## View the diagrams

From the repository root:

```bash
npx likec4@1.59.2 start docs/architecture
```

Or from this folder:

```bash
cd docs/architecture
npx likec4@1.59.2 start .
```

Opens a local preview (browser). Use the view picker to navigate.

### Validate

```bash
npx likec4@1.59.2 validate --json --no-layout docs/architecture
```

### Export (optional)

```bash
npx likec4@1.59.2 export png -o docs/architecture/export docs/architecture
```

### VS Code / Cursor

Install the [LikeC4](https://marketplace.visualstudio.com/items?itemName=likec4.likec4-vscode) extension and open any `.c4` file under this directory.

## Source files

| File | Role |
|------|------|
| `likec4.config.json` | Project config (`name`: manager-api) |
| `specification.c4` | Element / relationship kinds and tags |
| `model-context.c4` | Persons, systems, PostgreSQL |
| `model-dotnet.c4` | .NET containers + VSA components |
| `model-python.c4` | Python containers + VSA components |
| `views-structure.c4` | Context, container, component views |
| `views-flows.c4` | Dynamic views: login, refresh, JWT, CRUD |

## Views

### Structural

| View ID | Title | Level |
|---------|-------|-------|
| `index` | Manager API — Index | Landscape |
| `context` | System Context | Context |
| `containers-overview` | Containers — Both Stacks | Container |
| `containers-dotnet` | .NET — Containers | Container |
| `containers-python` | Python — Containers | Container |
| `components-dotnet` | .NET — Components (VSA) | Component |
| `components-python` | Python — Components (VSA) | Component |

### Flows (dynamic / sequence)

| View ID | Title |
|---------|-------|
| `login-dotnet` | Login Flow (.NET) |
| `login-python` | Login Flow (Python) |
| `refresh-dotnet` | Refresh Token Flow (.NET) |
| `jwt-protected-dotnet` | JWT Auth / Protected Route (.NET) |
| `jwt-protected-python` | JWT Auth / Protected Route (Python) |
| `crud-create-dotnet` | User CRUD — Create (.NET) |
| `crud-create-python` | User CRUD — Create (Python) |
| `crud-read-dotnet` | User CRUD — Read / Search (.NET) |
| `crud-read-python` | User CRUD — Read / Search (Python) |

## Modeling notes

- **Two systems** (`.NET` and `Python`) are alternative implementations of the same API contract — not a distributed pair you must run together.
- Each stack is **one API host process** + **its own PostgreSQL database** (same engine, different DB names).
- VSA folders (`Features` / `features`, `Common`, `Database`, `Authentication`) are **components inside the host**, not separate deployables.
- HTTP failures use **Problem Details** with stable codes (`Users.NotFound`, `Auth.InvalidCredentials`, etc.).
- `#target` tags mark authentication components (JWT + Argon2).

## Limitations

- LikeC4 MCP (`user-likec4`) was unavailable during authoring; validation used the CLI only.
- No deployment topology (local/dev/prod nodes) — add a `deployment { }` block later if needed.
- Update/Delete CRUD follow the same slice pattern as Create/Read; only Create and Read sequences are drawn to avoid duplication.
