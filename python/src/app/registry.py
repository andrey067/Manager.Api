"""Register VSA feature routers on the FastAPI application."""

from __future__ import annotations

from fastapi import FastAPI

from features.auth.login import router as login_router
from features.auth.refresh_token import router as refresh_token_router


def register_routes(app: FastAPI) -> None:
    app.include_router(login_router)
    app.include_router(refresh_token_router)
