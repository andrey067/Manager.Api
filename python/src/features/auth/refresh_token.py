"""RefreshToken slice — rotate JWT + refresh token pair."""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime

from fastapi import APIRouter, Depends
from pydantic import BaseModel, ConfigDict, Field
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.errors import AuthErrors
from authentication.token_service import JwtTokenService
from common.clock import Clock, SystemClock
from common.problem import problem_response
from common.result import Result
from database.models import UserModel
from database.session import get_db
from features.users.entity import User
from features.users.events import UserTokenRefreshedDomainEvent

router = APIRouter(prefix="/api/v1/auth", tags=["Auth"])


@dataclass(frozen=True, slots=True)
class Command:
    refresh_token: str


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
        if not command.refresh_token.strip():
            errors.append(ValidationError("refreshToken", "Refresh token is required."))
        return errors


class RefreshTokenRequest(BaseModel):
    refresh_token: str = Field(validation_alias="refreshToken")


class RefreshTokenResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    access_token: str = Field(serialization_alias="accessToken")
    access_token_expires: datetime = Field(serialization_alias="accessTokenExpires")
    refresh_token: str = Field(serialization_alias="refreshToken")
    refresh_token_expires: datetime = Field(serialization_alias="refreshTokenExpires")


class Handler:
    def __init__(
        self,
        session: AsyncSession,
        token_service: JwtTokenService,
        clock: Clock | None = None,
    ) -> None:
        self._session = session
        self._tokens = token_service
        self._clock = clock or SystemClock()

    async def handle(self, command: Command) -> Result[Response]:
        token_hash = self._tokens.hash_refresh_token(command.refresh_token)
        now = self._clock.utc_now()
        stored_now = now.replace(tzinfo=None) if now.tzinfo is not None else now

        result = await self._session.execute(
            select(UserModel).where(
                UserModel.refresh_token_hash == token_hash,
                UserModel.refresh_token_expires_at > stored_now,
            )
        )
        user = result.scalar_one_or_none()

        if user is None:
            return Result.failure(AuthErrors.invalid_refresh_token())

        access_token, access_expires = self._tokens.create_access_token(
            user.id, user.email
        )
        refresh_token = self._tokens.create_refresh_token()
        refresh_expires = self._tokens.get_refresh_expiry()

        user.refresh_token_hash = self._tokens.hash_refresh_token(refresh_token)
        user.refresh_token_expires_at = refresh_expires.replace(tzinfo=None)

        domain_user = User.from_persistence(
            id=user.id,
            name=user.name,
            email=user.email,
            password=user.password,
            refresh_token_hash=user.refresh_token_hash,
            refresh_token_expires_at=user.refresh_token_expires_at,
        )
        domain_user.raise_event(UserTokenRefreshedDomainEvent(id=domain_user.id))
        await self._session.commit()

        return Result.success(
            Response(
                access_token=access_token,
                access_token_expires=access_expires,
                refresh_token=refresh_token,
                refresh_token_expires=refresh_expires,
            )
        )


def _get_token_service() -> JwtTokenService:
    from authentication.config import get_jwt_settings

    return JwtTokenService(get_jwt_settings(), clock=SystemClock())


def _get_handler(
    session: AsyncSession = Depends(get_db),
    token_service: JwtTokenService = Depends(_get_token_service),
) -> Handler:
    return Handler(session, token_service)


@router.post("/refresh", response_model=None)
async def refresh(
    body: RefreshTokenRequest,
    handler: Handler = Depends(_get_handler),
) -> RefreshTokenResponse | object:
    command = Command(refresh_token=body.refresh_token)
    validation_errors = Validator().validate(command)
    if validation_errors:
        from common.error import Error

        detail = "; ".join(e.message for e in validation_errors)
        return problem_response(Error.validation("Validation.Error", detail))

    result = await handler.handle(command)
    return result.match(
        on_success=lambda value: RefreshTokenResponse(
            access_token=value.access_token,
            access_token_expires=value.access_token_expires,
            refresh_token=value.refresh_token,
            refresh_token_expires=value.refresh_token_expires,
        ),
        on_failure=problem_response,
    )
