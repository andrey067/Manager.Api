"""SearchUsersByName slice — search users by name contains when authenticated."""

from __future__ import annotations

from dataclasses import dataclass

from fastapi import APIRouter, Depends
from fastapi import Query as FastApiQuery
from pydantic import BaseModel, ConfigDict
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession

from authentication.dependencies import get_current_subject
from common.problem import problem_response
from common.result import Result
from database.models import UserModel
from database.session import get_db

router = APIRouter(prefix="/api/v1/users", tags=["Users"])


@dataclass(frozen=True, slots=True)
class Query:
    name: str


@dataclass(frozen=True, slots=True)
class Response:
    id: int
    name: str
    email: str


class SearchUsersByNameResponseItem(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: int
    name: str
    email: str


class Handler:
    def __init__(self, session: AsyncSession) -> None:
        self._session = session

    async def handle(self, query: Query) -> Result[list[Response]]:
        models = (
            (
                await self._session.execute(
                    select(UserModel)
                    .where(func.lower(UserModel.name).contains(query.name.lower()))
                    .order_by(UserModel.id)
                )
            )
            .scalars()
            .all()
        )
        users = [
            Response(id=model.id, name=model.name, email=model.email)
            for model in models
        ]
        return Result.success(users)


def _get_handler(session: AsyncSession = Depends(get_db)) -> Handler:
    return Handler(session)


@router.get("/search-by-name", response_model=None)
async def search_users_by_name(
    name: str = FastApiQuery(...),
    _: int = Depends(get_current_subject),
    handler: Handler = Depends(_get_handler),
) -> list[SearchUsersByNameResponseItem] | object:
    result = await handler.handle(Query(name=name))
    return result.match(
        on_success=lambda value: [
            SearchUsersByNameResponseItem(id=user.id, name=user.name, email=user.email)
            for user in value
        ],
        on_failure=problem_response,
    )
