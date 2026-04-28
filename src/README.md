# Manager API — FastAPI (Clean Architecture + TDD)

A Python/FastAPI implementation of the Manager API, built with Clean Architecture and TDD.

## Structure

```
frameworks/fastapi/
├── src/
│   ├── domain/          # Entities + validators (no external deps)
│   ├── application/     # Services, DTOs, repository interfaces
│   ├── infrastructure/  # SQLAlchemy models + repositories
│   └── api/             # FastAPI routers + Pydantic schemas
├── tests/
│   ├── unit/            # Domain + service tests (mocked deps)
│   └── integration/     # Full HTTP stack with in-memory SQLite
├── requirements.txt
└── pytest.ini
```

## Setup

```bash
cd frameworks/fastapi
pip install -r requirements.txt
```

## Run tests

```bash
# Unit tests only (fast, no DB)
pytest tests/unit/

# Integration tests
pytest tests/integration/

# All tests
pytest
```

## Run server

```bash
uvicorn src.api.main:app --reload
```

API docs available at `http://localhost:8000/docs`

## TDD Approach

1. **Domain tests first** (`tests/unit/test_user_entity.py`) — define entity behaviour before writing the entity.
2. **Service tests** (`tests/unit/test_user_service.py`) — mock repository, test orchestration logic.
3. **Integration tests** (`tests/integration/test_user_api.py`) — real HTTP + in-memory SQLite.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | /health | Health check |
| POST | /api/v1/users/ | Create user |
| GET | /api/v1/users/ | List all users |
| GET | /api/v1/users/{id} | Get user by ID |
| PUT | /api/v1/users/{id} | Update user |
| DELETE | /api/v1/users/{id} | Delete user |
| GET | /api/v1/users/search/name?name= | Search by name |
| GET | /api/v1/users/search/email?email= | Search by email |
