"""Repository interface — defines the contract for persistence."""
from abc import ABC, abstractmethod
from typing import Optional, List
from src.domain.entities import User


class IUserRepository(ABC):
    @abstractmethod
    async def create(self, user: User) -> User: ...

    @abstractmethod
    async def update(self, user: User) -> User: ...

    @abstractmethod
    async def delete(self, id: int) -> None: ...

    @abstractmethod
    async def get_by_id(self, id: int) -> Optional[User]: ...

    @abstractmethod
    async def get_all(self) -> List[User]: ...

    @abstractmethod
    async def get_by_email(self, email: str) -> Optional[User]: ...

    @abstractmethod
    async def search_by_name(self, name: str) -> List[User]: ...

    @abstractmethod
    async def search_by_email(self, email: str) -> List[User]: ...
