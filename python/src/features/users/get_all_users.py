"""GetAllUsers slice — list all users when authenticated."""

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

router = APIRouter(prefix="/api/v1/users", tags=["Users"])

_CACHE_TTL_SECONDS = 60.0


@dataclass(frozen=True, slots=True)
class Query:
    pass


@dataclass(frozen=True, slots=True)
class Response:
    id: int
    name: str
    email: str


class GetAllUsersResponseItem(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: int
    name: str
    email: str


class Handler:
    def __init__(self, session: AsyncSession, cache: AppCache) -> None:
        self._session = session
        self._cache = cache

    async def handle(self, query: Query) -> Result[list[Response]]:
        async def factory() -> list[Response]:
            models = (
                await self._session.execute(
                    select(UserModel).order_by(UserModel.id)
                )
            ).scalars().all()
            return [
                Response(id=model.id, name=model.name, email=model.email)
                for model in models
            ]

        users = await self._cache.get_or_set(
            UserCacheKeys.ALL,
            factory,
            ttl_seconds=_CACHE_TTL_SECONDS,
        )
        return Result.success(users)


def _get_handler(
    session: AsyncSession = Depends(get_db),
    cache: AppCache = Depends(get_app_cache),
) -> Handler:
    return Handler(session, cache)


@router.get("", response_model=None)
async def get_all_users(
    _: int = Depends(get_current_subject),
    handler: Handler = Depends(_get_handler),
) -> list[GetAllUsersResponseItem] | object:
    result = await handler.handle(Query())
    return result.match(
        on_success=lambda value: [
            GetAllUsersResponseItem(id=user.id, name=user.name, email=user.email)
            for user in value
        ],
        on_failure=problem_response,
    )
