"""UpdateUser slice — update an existing user when authenticated."""

from __future__ import annotations

import re
from dataclasses import dataclass

from fastapi import APIRouter, Depends
from pydantic import BaseModel, ConfigDict
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
from features.users.events import UserUpdatedDomainEvent

router = APIRouter(prefix="/api/v1/users", tags=["Users"])

_EMAIL_PATTERN = re.compile(
    r"^([\w\-.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))"
    r"([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"
)


@dataclass(frozen=True, slots=True)
class Command:
    id: int
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


class UpdateUserRequest(BaseModel):
    name: str
    email: str
    password: str


class UpdateUserResponse(BaseModel):
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
        model = (
            await self._session.execute(
                select(UserModel).where(UserModel.id == command.id)
            )
        ).scalar_one_or_none()
        if model is None:
            return Result.failure(UserErrors.not_found())

        email = command.email.strip()
        conflict = (
            await self._session.execute(
                select(UserModel).where(
                    UserModel.email == email,
                    UserModel.id != command.id,
                )
            )
        ).scalar_one_or_none()
        if conflict is not None:
            return Result.failure(UserErrors.email_conflict())

        old_email = model.email
        name = command.name.strip()

        model.name = name
        model.email = email
        if not self._hasher.verify(command.password, model.password):
            model.password = self._hasher.hash(command.password)

        await self._session.commit()
        await self._session.refresh(model)

        persisted_user = User.from_persistence(
            id=model.id,
            name=model.name,
            email=model.email,
            password=model.password,
        )
        persisted_user.raise_event(UserUpdatedDomainEvent(id=persisted_user.id))

        await self._cache.remove(UserCacheKeys.by_id(model.id))
        await self._cache.remove(UserCacheKeys.by_email(old_email))
        await self._cache.remove(UserCacheKeys.by_email(email))
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


@router.put("/{user_id}", response_model=None)
async def update_user(
    user_id: int,
    body: UpdateUserRequest,
    _: int = Depends(get_current_subject),
    handler: Handler = Depends(_get_handler),
) -> UpdateUserResponse | object:
    command = Command(
        id=user_id,
        name=body.name,
        email=body.email,
        password=body.password,
    )
    validation_errors = Validator().validate(command)
    if validation_errors:
        from common.error import Error

        detail = "; ".join(e.message for e in validation_errors)
        return problem_response(Error.validation("Validation.Error", detail))

    normalized = Command(
        id=command.id,
        name=command.name.strip(),
        email=command.email.strip(),
        password=command.password,
    )
    result = await handler.handle(normalized)
    return result.match(
        on_success=lambda value: UpdateUserResponse(
            id=value.id,
            name=value.name,
            email=value.email,
        ),
        on_failure=problem_response,
    )
