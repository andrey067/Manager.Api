"""Database session factory."""
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, Session
from src.infrastructure.models.user_model import Base

DATABASE_URL = "sqlite:///./manager.db"

engine = create_engine(DATABASE_URL, connect_args={"check_same_thread": False})
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)


def create_tables():
    Base.metadata.create_all(bind=engine)


def get_session() -> Session:
    session = SessionLocal()
    try:
        yield session
    finally:
        session.close()
