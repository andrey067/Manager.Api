"""SQLAlchemy ORM model for User."""
from sqlalchemy import Column, Integer, String
from sqlalchemy.orm import DeclarativeBase


class Base(DeclarativeBase):
    pass


class UserModel(Base):
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, autoincrement=True)
    name = Column(String(80), nullable=False)
    email = Column(String(180), nullable=False, unique=True)
    password = Column(String(255), nullable=False)
