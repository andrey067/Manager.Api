"""GetUserByEmail slice — fetch a user by exact email when authenticated."""

from __future__ import annotations

from dataclasses import dataclass

from fastapi import APIRouter, Depends, Query
from pydantic import BaseModel, ConfigDict
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.dependencies import get_current_subject
from common.cache import AppCache
from common.problem import problem_response
from common.result import Result
from database.models import UserModel
from database.session import get_db
from features.users.cache_keys import UserCacheKeys
from features.users.create_user import get_app_cache
from features.users.errors import UserErrors

router = APIRouter(prefix="/api/v1/users", tags=["Users"])

_CACHE_TTL_SECONDS = 60.0


@dataclass(frozen=True, slots=True)
class Query:
    email: str


@dataclass(frozen=True, slots=True)
class Response:
    id: int
    name: str
    email: str


class _UserNotFound(Exception):
    pass


class GetUserByEmailResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: int
    name: str
    email: str


class Handler:
    def __init__(self, session: AsyncSession, cache: AppCache) -> None:
        self._session = session
        self._cache = cache

    async def handle(self, query: Query) -> Result[Response]:
        cache_key = UserCacheKeys.by_email(query.email)

        async def factory() -> Response:
            model = (
                await self._session.execute(
                    select(UserModel).where(
                        func.lower(UserModel.email) == query.email.lower()
                    )
                )
            ).scalar_one_or_none()
            if model is None:
                raise _UserNotFound()
            return Response(id=model.id, name=model.name, email=model.email)

        try:
            response = await self._cache.get_or_set(
                cache_key,
                factory,
                ttl_seconds=_CACHE_TTL_SECONDS,
            )
        except _UserNotFound:
            return Result.failure(UserErrors.not_found())

        return Result.success(response)


def _get_handler(
    session: AsyncSession = Depends(get_db),
    cache: AppCache = Depends(get_app_cache),
) -> Handler:
    return Handler(session, cache)


@router.get("/by-email", response_model=None)
async def get_user_by_email(
    email: str = Query(...),
    _: int = Depends(get_current_subject),
    handler: Handler = Depends(_get_handler),
) -> GetUserByEmailResponse | object:
    result = await handler.handle(Query(email=email))
    return result.match(
        on_success=lambda value: GetUserByEmailResponse(
            id=value.id,
            name=value.name,
            email=value.email,
        ),
        on_failure=problem_response,
    )
