"""Register VSA feature routers on the FastAPI application."""

from __future__ import annotations

from fastapi import FastAPI

from features.auth.login import router as login_router
from features.auth.refresh_token import router as refresh_token_router
from features.users.create_user import router as create_user_router
from features.users.get_all_users import router as get_all_users_router
from features.users.get_user import router as get_user_router
from features.users.get_user_by_email import router as get_user_by_email_router
from features.users.register_bootstrap import router as register_bootstrap_router
from features.users.remove_user import router as remove_user_router
from features.users.search_users_by_email import router as search_users_by_email_router
from features.users.search_users_by_name import router as search_users_by_name_router
from features.users.update_user import router as update_user_router


def register_routes(app: FastAPI) -> None:
    app.include_router(login_router)
    app.include_router(refresh_token_router)
    app.include_router(register_bootstrap_router)
    app.include_router(create_user_router)
    app.include_router(get_all_users_router)
    app.include_router(get_user_by_email_router)
    app.include_router(search_users_by_name_router)
    app.include_router(search_users_by_email_router)
    app.include_router(get_user_router)
    app.include_router(update_user_router)
    app.include_router(remove_user_router)
