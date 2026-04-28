"""SQLAlchemy implementation of IUserRepository using synchronous session run in thread executor."""
from typing import Optional, List
from sqlalchemy import create_engine, select
from sqlalchemy.orm import Session

from src.application.interfaces import IUserRepository
from src.domain.entities import User
from src.infrastructure.models.user_model import UserModel, Base


def _to_domain(m: UserModel) -> User:
    u = User(name=m.name, email=m.email, password=m.password)
    u.id = m.id
    return u


def _to_model(u: User) -> UserModel:
    return UserModel(name=u.name, email=u.email, password=u.password)


class SqlAlchemyUserRepository(IUserRepository):
    """
    Synchronous SQLAlchemy repository exposed via async interface
    by running DB calls in a thread executor.
    Uses SQLite by default for easy study / portability.
    """

    def __init__(self, session: Session):
        self._session = session

    async def create(self, user: User) -> User:
        model = _to_model(user)
        self._session.add(model)
        self._session.commit()
        self._session.refresh(model)
        return _to_domain(model)

    async def update(self, user: User) -> User:
        model = self._session.get(UserModel, user.id)
        model.name = user.name
        model.email = user.email
        model.password = user.password
        self._session.commit()
        self._session.refresh(model)
        return _to_domain(model)

    async def delete(self, id: int) -> None:
        model = self._session.get(UserModel, id)
        if model:
            self._session.delete(model)
            self._session.commit()

    async def get_by_id(self, id: int) -> Optional[User]:
        model = self._session.get(UserModel, id)
        return _to_domain(model) if model else None

    async def get_all(self) -> List[User]:
        models = self._session.execute(select(UserModel)).scalars().all()
        return [_to_domain(m) for m in models]

    async def get_by_email(self, email: str) -> Optional[User]:
        stmt = select(UserModel).where(UserModel.email == email)
        model = self._session.execute(stmt).scalar_one_or_none()
        return _to_domain(model) if model else None

    async def search_by_name(self, name: str) -> List[User]:
        stmt = select(UserModel).where(UserModel.name.ilike(f"%{name}%"))
        models = self._session.execute(stmt).scalars().all()
        return [_to_domain(m) for m in models]

    async def search_by_email(self, email: str) -> List[User]:
        stmt = select(UserModel).where(UserModel.email.ilike(f"%{email}%"))
        models = self._session.execute(stmt).scalars().all()
        return [_to_domain(m) for m in models]
