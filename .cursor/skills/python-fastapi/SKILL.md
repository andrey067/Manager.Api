---
name: python-fastapi
description: Cria endpoints com FastAPI, routers por feature e Pydantic. Use ao criar ou alterar APIs REST, rotas ou quando o usuário menciona FastAPI, endpoints ou routers.
---

# FastAPI neste Projeto

## Estrutura de Routers

```
src/
  api/
    routers/
      users.py
      auth.py
    main.py
```

## Padrão de Router

```python
from fastapi import APIRouter, Depends, status

router = APIRouter(prefix="/api/v1/users", tags=["users"])

@router.get("/{id}", response_model=UserResponse)
async def get_user(
    id: int,
    service: UserService = Depends(get_user_service),
) -> UserResponse:
    user = await service.get(id)
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    return user

@router.post("/", status_code=status.HTTP_201_CREATED)
async def create_user(
    payload: UserCreate,
    service: UserService = Depends(get_user_service),
) -> UserResponse:
    return await service.create(payload)

@router.put("/{id}")
@router.delete("/{id}")
```

## Schemas Pydantic

```python
from pydantic import BaseModel, EmailStr

class UserCreate(BaseModel):
    name: str
    email: EmailStr

class UserResponse(BaseModel):
    id: int
    name: str
    email: str

    model_config = {"from_attributes": True}
```

## Registro no main.py

```python
from fastapi import FastAPI
from src.api.routers import users, auth

app = FastAPI()
app.include_router(users.router)
app.include_router(auth.router)
```

## Dependency Injection

Use `Depends()` para injetar serviços e repositórios. Evite lógica de negócio dentro dos endpoints.
