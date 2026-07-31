"""RegisterBootstrap slice — create the first user when the table is empty."""

from __future__ import annotations

import re
from dataclasses import dataclass

from fastapi import APIRouter, Depends
from pydantic import BaseModel, ConfigDict, Field
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

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

_EMAIL_PATTERN = re.compile(
    r"^([\w\-.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))"
    r"([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"
)


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


@dataclass(frozen=True, slots=True)
class ValidationError:
    field: str
    message: str


class Validator:
    def validate(self, command: Command) -> list[ValidationError]:
        errors: list[ValidationError] = []

        name = command.name.strip()
        if len(name) < 2:
            errors.append(ValidationError("name", "Name must be at least 2 characters."))
        elif len(name) > 80:
            errors.append(ValidationError("name", "Name must be at most 80 characters."))

        email = command.email.strip()
        if not email:
            errors.append(ValidationError("email", "Email is required."))
        elif len(email) > 180:
            errors.append(ValidationError("email", "Email must be at most 180 characters."))
        elif not _EMAIL_PATTERN.match(email):
            errors.append(ValidationError("email", "Email is not valid."))

        password = command.password
        if len(password) < 8:
            errors.append(ValidationError("password", "Password must be at least 8 characters."))
        elif len(password) > 30:
            errors.append(ValidationError("password", "Password must be at most 30 characters."))

        return errors


class BootstrapRequest(BaseModel):
    name: str
    email: str
    password: str


class BootstrapResponse(BaseModel):
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
        count = (
            await self._session.execute(select(func.count()).select_from(UserModel))
        ).scalar_one()
        if count > 0:
            return Result.failure(UserErrors.bootstrap_not_allowed())

        name = command.name.strip()
        email = command.email.strip()
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


@router.post("/bootstrap", response_model=None)
async def register_bootstrap(
    body: BootstrapRequest,
    handler: Handler = Depends(_get_handler),
) -> BootstrapResponse | object:
    command = Command(name=body.name, email=body.email, password=body.password)
    validation_errors = Validator().validate(command)
    if validation_errors:
        from common.error import Error

        detail = "; ".join(e.message for e in validation_errors)
        return problem_response(Error.validation("Validation.Error", detail))

    normalized = Command(
        name=command.name.strip(),
        email=command.email.strip(),
        password=command.password,
    )
    result = await handler.handle(normalized)
    return result.match(
        on_success=lambda value: BootstrapResponse(
            id=value.id,
            name=value.name,
            email=value.email,
        ),
        on_failure=problem_response,
    )
