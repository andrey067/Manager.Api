"""FastAPI dependencies for JWT-protected VSA routes."""

from __future__ import annotations

from fastapi import Depends, HTTPException, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from authentication.config import get_jwt_settings
from authentication.token_service import JwtTokenService
from common.clock import SystemClock

_bearer = HTTPBearer(auto_error=False)


def _get_token_service() -> JwtTokenService:
    return JwtTokenService(get_jwt_settings(), clock=SystemClock())


async def get_current_subject(
    credentials: HTTPAuthorizationCredentials | None = Depends(_bearer),
    token_service: JwtTokenService = Depends(_get_token_service),
) -> int:
    """Require a valid Bearer JWT; return authenticated user id."""
    if credentials is None or credentials.scheme.lower() != "bearer":
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Not authenticated",
            headers={"WWW-Authenticate": "Bearer"},
        )
    user_id = token_service.validate_access_token(credentials.credentials)
    if user_id is None:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid or expired token",
            headers={"WWW-Authenticate": "Bearer"},
        )
    return user_id
