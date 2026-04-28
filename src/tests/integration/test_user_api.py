"""Integration tests — full HTTP stack with in-memory SQLite."""
import pytest
from fastapi.testclient import TestClient
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

from src.api.main import create_app
from src.infrastructure.database import get_session
from src.infrastructure.models.user_model import Base

TEST_DATABASE_URL = "sqlite:///./test_manager.db"


@pytest.fixture(scope="module")
def test_app():
    engine = create_engine(TEST_DATABASE_URL, connect_args={"check_same_thread": False})
    TestingSession = sessionmaker(autocommit=False, autoflush=False, bind=engine)
    Base.metadata.create_all(bind=engine)

    app = create_app()

    def override_session():
        session = TestingSession()
        try:
            yield session
        finally:
            session.close()

    app.dependency_overrides[get_session] = override_session
    yield TestClient(app)

    Base.metadata.drop_all(bind=engine)
    engine.dispose()


class TestUserIntegration:
    def test_health(self, test_app):
        r = test_app.get("/health")
        assert r.status_code == 200

    def test_create_user(self, test_app):
        r = test_app.post("/api/v1/users/", json={
            "name": "John Doe",
            "email": "john@example.com",
            "password": "secret123",
        })
        assert r.status_code == 201
        data = r.json()
        assert data["email"] == "john@example.com"
        assert "id" in data

    def test_create_duplicate_user_returns_409(self, test_app):
        test_app.post("/api/v1/users/", json={
            "name": "Dup User",
            "email": "dup@example.com",
            "password": "secret123",
        })
        r = test_app.post("/api/v1/users/", json={
            "name": "Dup User",
            "email": "dup@example.com",
            "password": "secret123",
        })
        assert r.status_code == 409

    def test_get_user(self, test_app):
        create_r = test_app.post("/api/v1/users/", json={
            "name": "Get Me",
            "email": "getme@example.com",
            "password": "secret123",
        })
        user_id = create_r.json()["id"]
        r = test_app.get(f"/api/v1/users/{user_id}")
        assert r.status_code == 200
        assert r.json()["id"] == user_id

    def test_update_user(self, test_app):
        create_r = test_app.post("/api/v1/users/", json={
            "name": "Old Name",
            "email": "update@example.com",
            "password": "secret123",
        })
        user_id = create_r.json()["id"]
        r = test_app.put(f"/api/v1/users/{user_id}", json={
            "name": "New Name",
            "email": "update@example.com",
            "password": "secret123",
        })
        assert r.status_code == 200
        assert r.json()["name"] == "New Name"

    def test_delete_user(self, test_app):
        create_r = test_app.post("/api/v1/users/", json={
            "name": "Delete Me",
            "email": "delete@example.com",
            "password": "secret123",
        })
        user_id = create_r.json()["id"]
        r = test_app.delete(f"/api/v1/users/{user_id}")
        assert r.status_code == 204
        r2 = test_app.get(f"/api/v1/users/{user_id}")
        assert r2.status_code == 404

    def test_get_all_users(self, test_app):
        r = test_app.get("/api/v1/users/")
        assert r.status_code == 200
        assert isinstance(r.json(), list)
