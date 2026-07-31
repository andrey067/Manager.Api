"""RemoveUser slice — delete a user when authenticated."""

from __future__ import annotations

from dataclasses import dataclass

from fastapi import APIRouter, Depends, Response
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
from features.users.entity import User
from features.users.errors import UserErrors
from features.users.events import UserRemovedDomainEvent

router = APIRouter(prefix="/api/v1/users", tags=["Users"])


@dataclass(frozen=True, slots=True)
class Command:
    id: int


class Handler:
    def __init__(self, session: AsyncSession, cache: AppCache) -> None:
        self._session = session
        self._cache = cache

    async def handle(self, command: Command) -> Result[None]:
        model = (
            await self._session.execute(
                select(UserModel).where(UserModel.id == command.id)
            )
        ).scalar_one_or_none()
        if model is None:
            return Result.failure(UserErrors.not_found())

        email = model.email
        user_id = model.id

        removed_user = User.from_persistence(
            id=model.id,
            name=model.name,
            email=model.email,
            password=model.password,
        )
        removed_user.raise_event(UserRemovedDomainEvent(id=removed_user.id))

        await self._session.delete(model)
        await self._session.commit()

        await self._cache.remove(UserCacheKeys.by_id(user_id))
        await self._cache.remove(UserCacheKeys.by_email(email))
        await self._cache.remove(UserCacheKeys.ALL)

        return Result.success()


def _get_handler(
    session: AsyncSession = Depends(get_db),
    cache: AppCache = Depends(get_app_cache),
) -> Handler:
    return Handler(session, cache)


@router.delete("/{user_id}", response_model=None, status_code=204)
async def remove_user(
    user_id: int,
    _: int = Depends(get_current_subject),
    handler: Handler = Depends(_get_handler),
) -> Response | object:
    result = await handler.handle(Command(id=user_id))
    return result.match(
        on_success=lambda: Response(status_code=204),
        on_failure=problem_response,
    )
