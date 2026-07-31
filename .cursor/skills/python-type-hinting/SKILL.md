---
name: python-type-hinting
description: Type hints com ruff, mypy, pyright e Pydantic. Use ao configurar ferramentas, validar tipos ou quando o usuário menciona type hints.
---

# Type Hinting neste Projeto Python

## Ferramentas

- **Ruff** – linter (pycodestyle, Pyflakes, isort, pyupgrade) e formatter
- **mypy** – checagem estática de tipos
- **pyright** – checagem de tipos no editor (Pylance)
- **Pydantic** – validação em runtime e schemas tipados

## Configuração em pyproject.toml

```toml
[tool.pyright]
extraPaths = ["src"]

[tool.ruff]
target-version = "py312"
line-length = 88
src = ["src", "tests"]

[tool.ruff.lint]
select = ["E", "W", "F", "I", "B", "C4", "UP"]
ignore = ["E501"]

[tool.mypy]
python_version = "3.12"
warn_return_any = true
warn_unused_configs = true
disallow_untyped_defs = false

[[tool.mypy.overrides]]
mypy_path = "src"
```

## Padrões de Tipagem

1. **Funções**: sempre anotar parâmetros e retorno
   ```python
   def get_user(id: int) -> User | None: ...
   ```

2. **Variáveis**: anotar quando o tipo não é óbvio
   ```python
   users: list[User] = []
   ```

3. **Pydantic**: usar BaseModel para request/response
   ```python
   class UserCreate(BaseModel):
       name: str
       email: EmailStr
   ```

4. **Protocol/ABC**: para interfaces e contratos
   ```python
   class UserRepository(Protocol):
       def get(self, id: int) -> User | None: ...
   ```
