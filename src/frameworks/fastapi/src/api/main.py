"""FastAPI application factory."""
from fastapi import FastAPI
from src.api.routers.user_router import router as user_router
from src.infrastructure.database import create_tables


def create_app() -> FastAPI:
    app = FastAPI(
        title="Manager API — FastAPI",
        description="Clean Architecture + TDD implementation in Python/FastAPI",
        version="1.0.0",
    )

    # Create tables on startup (for study/dev purposes)
    create_tables()

    app.include_router(user_router, prefix="/api/v1")

    @app.get("/health")
    def health():
        return {"status": "ok"}

    return app


app = create_app()
