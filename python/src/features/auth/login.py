"""Login slice — authenticate and issue JWT + refresh token."""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime

from fastapi import APIRouter, Depends
from pydantic import BaseModel, ConfigDict, Field
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.errors import AuthErrors
from authentication.password_hasher import Argon2PasswordHasher
from authentication.token_service import JwtTokenService
from common.clock import SystemClock
from common.problem import problem_response
from common.result import Result
from database.models import UserModel
from database.session import get_db

router = APIRouter(prefix="/api/v1/auth", tags=["Auth"])


@dataclass(frozen=True, slots=True)
class Command:
    login: str
    password: str


@dataclass(frozen=True, slots=True)
class Response:
    access_token: str
    access_token_expires: datetime
    refresh_token: str
    refresh_token_expires: datetime


@dataclass(frozen=True, slots=True)
class ValidationError:
    field: str
    message: str


class Validator:
    def validate(self, command: Command) -> list[ValidationError]:
        errors: list[ValidationError] = []
        if not command.login.strip():
            errors.append(ValidationError("login", "Login is required."))
        if not command.password:
            errors.append(ValidationError("password", "Password is required."))
        return errors


class LoginRequest(BaseModel):
    login: str
    password: str


class LoginResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    access_token: str = Field(serialization_alias="accessToken")
    access_token_expires: datetime = Field(serialization_alias="accessTokenExpires")
    refresh_token: str = Field(serialization_alias="refreshToken")
    refresh_token_expires: datetime = Field(serialization_alias="refreshTokenExpires")


class Handler:
    def __init__(
        self,
        session: AsyncSession,
        password_hasher: Argon2PasswordHasher,
        token_service: JwtTokenService,
    ) -> None:
        self._session = session
        self._hasher = password_hasher
        self._tokens = token_service

    async def handle(self, command: Command) -> Result[Response]:
        result = await self._session.execute(
            select(UserModel).where(UserModel.email == command.login)
        )
        user = result.scalar_one_or_none()

        if user is None or not self._hasher.verify(command.password, user.password):
            return Result.failure(AuthErrors.invalid_credentials())

        access_token, access_expires = self._tokens.create_access_token(
            user.id, user.email
        )
        refresh_token = self._tokens.create_refresh_token()
        refresh_expires = self._tokens.get_refresh_expiry()

        user.refresh_token_hash = self._tokens.hash_refresh_token(refresh_token)
        user.refresh_token_expires_at = refresh_expires
        await self._session.commit()

        return Result.success(
            Response(
                access_token=access_token,
                access_token_expires=access_expires,
                refresh_token=refresh_token,
                refresh_token_expires=refresh_expires,
            )
        )


def _get_password_hasher() -> Argon2PasswordHasher:
    from authentication.config import get_hash_settings

    return Argon2PasswordHasher(get_hash_settings())


def _get_token_service() -> JwtTokenService:
    from authentication.config import get_jwt_settings

    return JwtTokenService(get_jwt_settings(), clock=SystemClock())


def _get_handler(
    session: AsyncSession = Depends(get_db),
    password_hasher: Argon2PasswordHasher = Depends(_get_password_hasher),
    token_service: JwtTokenService = Depends(_get_token_service),
) -> Handler:
    return Handler(session, password_hasher, token_service)


@router.post("/login", response_model=None)
async def login(
    body: LoginRequest,
    handler: Handler = Depends(_get_handler),
) -> LoginResponse | object:
    command = Command(login=body.login, password=body.password)
    validation_errors = Validator().validate(command)
    if validation_errors:
        from common.error import Error

        detail = "; ".join(e.message for e in validation_errors)
        return problem_response(Error.validation("Validation.Error", detail))

    result = await handler.handle(command)
    return result.match(
        on_success=lambda value: LoginResponse(
            access_token=value.access_token,
            access_token_expires=value.access_token_expires,
            refresh_token=value.refresh_token,
            refresh_token_expires=value.refresh_token_expires,
        ),
        on_failure=problem_response,
    )
