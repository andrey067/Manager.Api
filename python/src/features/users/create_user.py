"""CreateUser slice — create a user when authenticated."""

from __future__ import annotations

from dataclasses import dataclass

from fastapi import APIRouter, Depends
from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.dependencies import get_current_subject
from authentication.password_hasher import Argon2PasswordHasher
from common.cache import AppCache
from common.problem import problem_response
from common.result import Result
from database.models import UserModel
from database.session import get_db
from features.users.cache_keys import UserCacheKeys
from features.users.dependencies import get_app_cache
from features.users.errors import UserErrors
from features.users.entity import User
from features.users.events import UserCreatedDomainEvent

router = APIRouter(prefix="/api/v1/users", tags=["Users"])


@dataclass(frozen=True, slots=True)
class Command:
    name: str
    email: str
    password: str


@dataclass(frozen=True, slots=True)
class Response:
    id: int
    name: str
    email: str


class CreateUserRequest(BaseModel):
    name: str = Field(min_length=2, max_length=80)
    email: EmailStr = Field(max_length=180)
    password: str = Field(min_length=8, max_length=30)

    @field_validator("name", "email", mode="before")
    @classmethod
    def strip_strings(cls, value: object) -> object:
        return value.strip() if isinstance(value, str) else value


class CreateUserResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: int
    name: str
    email: str


class Handler:
    def __init__(
        self,
        session: AsyncSession,
        password_hasher: Argon2PasswordHasher,
        cache: AppCache,
    ) -> None:
        self._session = session
        self._hasher = password_hasher
        self._cache = cache

    async def handle(self, command: Command) -> Result[Response]:
        email = command.email.strip()
        existing = (
            await self._session.execute(
                select(UserModel).where(UserModel.email == email)
            )
        ).scalar_one_or_none()
        if existing is not None:
            return Result.failure(UserErrors.email_conflict())

        name = command.name.strip()
        hashed = self._hasher.hash(command.password)
        model = UserModel(name=name, email=email, password=hashed)
        self._session.add(model)
        await self._session.commit()
        await self._session.refresh(model)

        persisted_user = User.from_persistence(
            id=model.id,
            name=model.name,
            email=model.email,
            password=model.password,
        )
        persisted_user.raise_event(UserCreatedDomainEvent(id=persisted_user.id))
        await self._cache.remove(UserCacheKeys.ALL)
        await self._cache.remove(UserCacheKeys.by_email(email))

        return Result.success(
            Response(
                id=persisted_user.id,
                name=persisted_user.name,
                email=persisted_user.email,
            )
        )


def _get_password_hasher() -> Argon2PasswordHasher:
    from authentication.config import get_hash_settings

    return Argon2PasswordHasher(get_hash_settings())


def _get_handler(
    session: AsyncSession = Depends(get_db),
    password_hasher: Argon2PasswordHasher = Depends(_get_password_hasher),
    cache: AppCache = Depends(get_app_cache),
) -> Handler:
    return Handler(session, password_hasher, cache)


@router.post("", response_model=None)
async def create_user(
    body: CreateUserRequest,
    _: int = Depends(get_current_subject),
    handler: Handler = Depends(_get_handler),
) -> CreateUserResponse | object:
    command = Command(name=body.name, email=body.email, password=body.password)
    result = await handler.handle(command)
    return result.match(
        on_success=lambda value: CreateUserResponse(
            id=value.id,
            name=value.name,
            email=value.email,
        ),
        on_failure=problem_response,
    )
