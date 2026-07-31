"""SearchUsersByEmail slice — search users by email contains when authenticated."""

from __future__ import annotations

from dataclasses import dataclass

from fastapi import APIRouter, Depends, Query
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
    email: str


@dataclass(frozen=True, slots=True)
class Response:
    id: int
    name: str
    email: str


class SearchUsersByEmailResponseItem(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: int
    name: str
    email: str


class Handler:
    def __init__(self, session: AsyncSession) -> None:
        self._session = session

    async def handle(self, query: Query) -> Result[list[Response]]:
        models = (
            await self._session.execute(
                select(UserModel)
                .where(func.lower(UserModel.email).contains(query.email.lower()))
                .order_by(UserModel.id)
            )
        ).scalars().all()
        users = [
            Response(id=model.id, name=model.name, email=model.email)
            for model in models
        ]
        return Result.success(users)


def _get_handler(session: AsyncSession = Depends(get_db)) -> Handler:
    return Handler(session)


@router.get("/search-by-email", response_model=None)
async def search_users_by_email(
    email: str = Query(...),
    _: int = Depends(get_current_subject),
    handler: Handler = Depends(_get_handler),
) -> list[SearchUsersByEmailResponseItem] | object:
    result = await handler.handle(Query(email=email))
    return result.match(
        on_success=lambda value: [
            SearchUsersByEmailResponseItem(id=user.id, name=user.name, email=user.email)
            for user in value
        ],
        on_failure=problem_response,
    )
