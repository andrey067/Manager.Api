# Task 17 Report: Cutover — remove legacy layered code

## Summary

Removed Clean Architecture layer projects and tests from both stacks. VSA is now the sole architecture.

## .NET

- **Deleted:** `src/1 - Manager.API` … `5 - Manager.Core`, `tests/Manager.{API,Domain,Services,Infra,Core}.Tests`, `Manager.Fixtures`, `Manager.IntegrationBase`
- **Kept:** `src/Manager.Api`, `tests/Manager.Vsa.Tests`, `Manager.Vsa.sln`
- **Updated:** `Manager.sln` now mirrors VSA projects only; `Directory.Build.props` coverage targets `[Manager.Api]` / `Manager.Vsa.Tests`; `.vscode/launch.json` + `tasks.json`; `scripts/test-with-coverage.sh`

## Python

- **Deleted:** `src/{domain,application,infrastructure,api,shared}`, `tests/{application,domain,infrastructure,api,shared}`
- **Kept:** `src/{app,authentication,common,database,features}`, `tests/{common,features,authentication}`, `tests/test_health_vsa.py`
- **Updated:** `pyproject.toml` (packages, coverage, isort, poe `run` → `app.main:app`), `alembic/env.py`, `scripts/seed_admin.py`

## Verification

```bash
cd dotnet && dotnet test Manager.Vsa.sln          # 118 passed
cd python && uv run pytest -v --no-cov            # 114 passed
```

Grep for `MediatR`, `DomainNotification`, `IUserRepository` in remaining `.cs`/`.py`: **no matches**.

## Commit

`refactor: cut over to VSA; remove Clean Architecture layers`

## Task 17 cutover fix (root legacy removal)

### Removed from git index (`git rm`)

- `Manager.sln` (repo root layered solution)
- `src/.vscode/launch.json`, `src/.vscode/tasks.json`
- `src/1 - Manager.API/` (entire project tree)
- `src/2 - Manager.Domain/` (entire project tree)
- `src/3 - Manager.Services/` (entire project tree)
- `src/4 - Manager.Infra/` (entire project tree, including EF migrations and `IUserRepository`)
- `src/5 - Manager.Core/` (entire project tree, including MediatR `DomainNotification` stack)
- `src/6 - Manager.Tests/` (entire project tree)

### Removed from disk only (untracked leftovers)

- `dotnet/tests/Manager.Tests/` (obsolete layered test project; not in git index)

### Package cleanup

- Removed unused `MediatR` central package version from `dotnet/Directory.Packages.props`

### Verification (this fix)

```bash
cd dotnet && dotnet test Manager.Vsa.sln   # 118 passed
cd python && uv run pytest -v --no-cov     # 114 passed
```

Grep `MediatR` / `DomainNotification` / `IUserRepository` in tracked `*.cs` / `*.py` / `*.csproj`: **no matches** (docs may still mention MediatR until Task 18).

### Commit

`fix: finish VSA cutover by removing legacy root src`
