---
name: python-namespaces
description: Estrutura src/, pacotes e namespaces em Python. Use ao organizar código, configurar imports ou quando o usuário menciona namespaces, packages ou src layout.
---

# Namespaces e Estrutura de Pacotes

## Layout src/

```
python/
  src/
    my_project/
      __init__.py
      domain/
        __init__.py
        user.py
      application/
        __init__.py
      infrastructure/
        __init__.py
      api/
        __init__.py
  tests/
  pyproject.toml
```

## Regras

1. **Pacote instalável**: `[tool.uv] package = true` em pyproject.toml
2. **Build**: hatchling com `packages = ["src"]` ou `src/my_project`
3. **pytest**: `pythonpath = ["src"]` para testes importarem o pacote

## Imports

- Prefira imports absolutos: `from my_project.domain.user import User`
- Evite imports circulares; dependências fluem domain → application → infrastructure → api

## Ruff isort

Configure `known-first-party` com o nome do pacote:

```toml
[tool.ruff.lint.isort]
known-first-party = ["my_project"]
```

## pyproject.toml – caminhos

```toml
[tool.pyright]
extraPaths = ["src"]

[tool.pytest.ini_options]
pythonpath = ["src"]
```
