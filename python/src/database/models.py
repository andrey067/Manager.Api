"""SQLAlchemy persistence models for the VSA stack."""

from __future__ import annotations

from datetime import datetime

from sqlalchemy import DateTime, String, UniqueConstraint
from sqlalchemy.orm import Mapped, mapped_column

from database.session import Base


class UserModel(Base):
    """Persistence model for User - table 'User' (PostgreSQL)."""

    __tablename__ = "User"
    __table_args__ = (UniqueConstraint("email", name="IX_User_Email"),)

    id: Mapped[int] = mapped_column(primary_key=True, autoincrement=True)
    name: Mapped[str] = mapped_column("name", String(80), nullable=False)
    email: Mapped[str] = mapped_column("email", String(180), nullable=False)
    password: Mapped[str] = mapped_column("password", String(1000), nullable=False)
    refresh_token_hash: Mapped[str | None] = mapped_column(
        "refresh_token_hash",
        String(128),
        nullable=True,
    )
    refresh_token_expires_at: Mapped[datetime | None] = mapped_column(
        "refresh_token_expires_at",
        DateTime(timezone=True),
        nullable=True,
    )
