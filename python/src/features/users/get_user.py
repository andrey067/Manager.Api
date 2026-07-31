"""GetUser slice — fetch a user by id when authenticated."""

from __future__ import annotations

from dataclasses import dataclass

from fastapi import APIRouter, Depends
from pydantic import BaseModel, ConfigDict
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.dependencies import get_current_subject
from common.cache import AppCache
from common.problem import problem_response
from common.result import Result
from database.models import UserModel
from database.session import get_db
from features.users.cache_keys import UserCacheKeys
from features.users.dependencies import get_app_cache
from features.users.errors import UserErrors

router = APIRouter(prefix="/api/v1/users", tags=["Users"])

_CACHE_TTL_SECONDS = 60.0


@dataclass(frozen=True, slots=True)
class Query:
    id: int


@dataclass(frozen=True, slots=True)
class Response:
    id: int
    name: str
    email: str


class _UserNotFound(Exception):
    pass


class GetUserResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: int
    name: str
    email: str


class Handler:
    def __init__(self, session: AsyncSession, cache: AppCache) -> None:
        self._session = session
        self._cache = cache

    async def handle(self, query: Query) -> Result[Response]:
        cache_key = UserCacheKeys.by_id(query.id)

        async def factory() -> Response:
            model = (
                await self._session.execute(
                    select(UserModel).where(UserModel.id == query.id)
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


@router.get("/{user_id}", response_model=None)
async def get_user(
    user_id: int,
    _: int = Depends(get_current_subject),
    handler: Handler = Depends(_get_handler),
) -> GetUserResponse | object:
    result = await handler.handle(Query(id=user_id))
    return result.match(
        on_success=lambda value: GetUserResponse(
            id=value.id,
            name=value.name,
            email=value.email,
        ),
        on_failure=problem_response,
    )
