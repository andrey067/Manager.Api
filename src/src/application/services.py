"""User application service — orchestrates domain + repository."""
from typing import List
from src.domain.entities import User
from src.application.interfaces import IUserRepository
from src.application.dtos import CreateUserDTO, UpdateUserDTO, UserDTO
from src.core.errors import DomainError, NotFoundError, ConflictError


def _to_dto(user: User) -> UserDTO:
    return UserDTO(id=user.id, name=user.name, email=user.email)


class UserService:
    def __init__(self, repository: IUserRepository):
        self._repo = repository

    async def create(self, dto: CreateUserDTO) -> UserDTO:
        # Check uniqueness
        existing = await self._repo.get_by_email(dto.email)
        if existing:
            raise ConflictError(f"Email '{dto.email}' is already registered.")

        user = User(name=dto.name, email=dto.email, password=dto.password)
        if not user.is_valid():
            raise DomainError(user.errors)

        saved = await self._repo.create(user)
        return _to_dto(saved)

    async def update(self, dto: UpdateUserDTO) -> UserDTO:
        existing = await self._repo.get_by_id(dto.id)
        if not existing:
            raise NotFoundError(f"User with id {dto.id} not found.")

        existing.set_name(dto.name)
        existing.set_email(dto.email)
        existing.set_password(dto.password)
        if not existing.is_valid():
            raise DomainError(existing.errors)

        saved = await self._repo.update(existing)
        return _to_dto(saved)

    async def delete(self, id: int) -> None:
        existing = await self._repo.get_by_id(id)
        if not existing:
            raise NotFoundError(f"User with id {id} not found.")
        await self._repo.delete(id)

    async def get_by_id(self, id: int) -> UserDTO:
        user = await self._repo.get_by_id(id)
        if not user:
            raise NotFoundError(f"User with id {id} not found.")
        return _to_dto(user)

    async def get_all(self) -> List[UserDTO]:
        users = await self._repo.get_all()
        return [_to_dto(u) for u in users]

    async def search_by_name(self, name: str) -> List[UserDTO]:
        users = await self._repo.search_by_name(name)
        return [_to_dto(u) for u in users]

    async def search_by_email(self, email: str) -> List[UserDTO]:
        users = await self._repo.search_by_email(email)
        return [_to_dto(u) for u in users]

    async def get_by_email(self, email: str) -> UserDTO:
        user = await self._repo.get_by_email(email)
        if not user:
            raise NotFoundError(f"User with email '{email}' not found.")
        return _to_dto(user)
