"""Register VSA feature routers on the FastAPI application."""

from __future__ import annotations

from fastapi import FastAPI

from features.auth.login import router as login_router


def register_routes(app: FastAPI) -> None:
    app.include_router(login_router)
