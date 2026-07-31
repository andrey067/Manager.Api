---
name: python-pyproject
description: Configura pyproject.toml com uv, hatchling, pytest, ruff, mypy e poe. Use ao criar ou ajustar o projeto Python, dependências ou tasks.
---

# pyproject.toml Padrão

## Template Base

Use este template como base. Ajuste `[project]` (name, description, dependencies) e `known-first-party` no Ruff.

```toml
# ========== Metadados (PEP 621) ==========
[project]
name = "my-project"
version = "0.1.0"
description = "Add your description here"
readme = "README.md"
requires-python = ">=3.12"
dependencies = []

# ========== uv ==========
[tool.uv]
package = true

# ========== Build ==========
[build-system]
requires = ["hatchling"]
build-backend = "hatchling.build"

[tool.hatch.build.targets.wheel]
packages = ["src"]

[tool.hatch.build.targets.sdist]
packages = ["src"]

# ========== Dependências de desenvolvimento ==========
[dependency-groups]
dev = [
    "poethepoet",
    "pytest",
    "ruff",
    "mypy",
]

# ========== Pytest ==========
[tool.pytest.ini_options]
pythonpath = ["src"]
testpaths = ["tests"]
python_files = ["test_*.py", "*_test.py"]
addopts = "-v --tb=short --ignore-glob=**/._*"

# ========== Pyright ==========
[tool.pyright]
extraPaths = ["src"]

# ========== Ruff ==========
[tool.ruff]
target-version = "py312"
line-length = 88
src = ["src", "tests"]
exclude = ["**/._*", ".venv"]

[tool.ruff.lint]
select = ["E", "W", "F", "I", "B", "C4", "UP"]
ignore = ["E501"]

[tool.ruff.lint.isort]
known-first-party = []

# ========== Mypy ==========
[tool.mypy]
python_version = "3.12"
warn_return_any = true
warn_unused_configs = true
disallow_untyped_defs = false

[[tool.mypy.overrides]]
mypy_path = "src"

# ========== Poe ==========
[tool.poe.tasks]
run = "uv run python -m src.main"
test = "uv run python -m pytest tests/ -v --tb=short"
lint = "ruff check src tests"
format = "ruff format src tests"
format-check = "ruff format --check src tests"
typecheck = "mypy src"
check = "poe lint && poe typecheck"
```

## Dependências de produção

Adicione em `[project].dependencies`, ex.:

```toml
dependencies = [
    "fastapi",
    "uvicorn[standard]",
    "pydantic",
]
```
