---
name: python-solid
description: Aplica princípios SOLID em Python. Use ao criar classes, interfaces ou quando o usuário menciona SOLID, design patterns ou boas práticas.
---

# Princípios SOLID em Python

## S – Single Responsibility

Cada classe/módulo com uma única responsabilidade.

```python
# Ruim: UserService faz persistência e envio de email
# Bom: UserService orquestra; UserRepository persiste; EmailService envia
```

## O – Open/Closed

Aberto para extensão, fechado para modificação. Use ABC, Protocol ou herança.

```python
from abc import ABC, abstractmethod

class PaymentProcessor(ABC):
    @abstractmethod
    def process(self, amount: float) -> bool: ...

class CreditCardProcessor(PaymentProcessor):
    def process(self, amount: float) -> bool: ...
```

## L – Liskov Substitution

Subtipos devem ser substituíveis por suas classes base sem quebrar o programa.

- Subclasses não devem enfraquecer pré-condições nem fortalecer pós-condições
- Evite sobrescrever métodos de forma que mude o contrato

## I – Interface Segregation

Interfaces menores e específicas. Em Python: `Protocol` por contrato.

```python
from typing import Protocol

class Readable(Protocol):
    def get(self, id: int) -> User | None: ...

class Writable(Protocol):
    def save(self, user: User) -> User: ...

# Repositório implementa ambos, mas quem só precisa ler usa Readable
```

## D – Dependency Inversion

Dependa de abstrações, não de implementações. Use `Depends()` do FastAPI para injeção.

```python
from fastapi import Depends

def get_user_service(
    repo: UserRepository = Depends(get_user_repository),
) -> UserService:
    return UserService(repo)

@router.get("/users/{id}")
def get_user(id: int, service: UserService = Depends(get_user_service)):
    ...
```
