"""User CRUD router."""
from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session
from typing import List

from src.api.schemas.user_schema import CreateUserRequest, UpdateUserRequest, UserResponse
from src.application.dtos import CreateUserDTO, UpdateUserDTO
from src.application.services import UserService
from src.infrastructure.database import get_session
from src.infrastructure.repositories.user_repository import SqlAlchemyUserRepository
from src.core.errors import DomainError, NotFoundError, ConflictError

router = APIRouter(prefix="/users", tags=["users"])


def get_service(session: Session = Depends(get_session)) -> UserService:
    repo = SqlAlchemyUserRepository(session)
    return UserService(repo)


@router.post("/", response_model=UserResponse, status_code=status.HTTP_201_CREATED)
async def create_user(body: CreateUserRequest, service: UserService = Depends(get_service)):
    try:
        dto = CreateUserDTO(name=body.name, email=body.email, password=body.password)
        return await service.create(dto)
    except ConflictError as e:
        raise HTTPException(status_code=409, detail=str(e))
    except DomainError as e:
        raise HTTPException(status_code=422, detail={"message": str(e), "errors": e.errors})


@router.put("/{user_id}", response_model=UserResponse)
async def update_user(user_id: int, body: UpdateUserRequest, service: UserService = Depends(get_service)):
    try:
        dto = UpdateUserDTO(id=user_id, name=body.name, email=body.email, password=body.password)
        return await service.update(dto)
    except NotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except DomainError as e:
        raise HTTPException(status_code=422, detail={"message": str(e), "errors": e.errors})


@router.delete("/{user_id}", status_code=status.HTTP_204_NO_CONTENT)
async def delete_user(user_id: int, service: UserService = Depends(get_service)):
    try:
        await service.delete(user_id)
    except NotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e))


@router.get("/{user_id}", response_model=UserResponse)
async def get_user(user_id: int, service: UserService = Depends(get_service)):
    try:
        return await service.get_by_id(user_id)
    except NotFoundError as e:
        raise HTTPException(status_code=404, detail=str(e))


@router.get("/", response_model=List[UserResponse])
async def get_all_users(service: UserService = Depends(get_service)):
    return await service.get_all()


@router.get("/search/name", response_model=List[UserResponse])
async def search_by_name(name: str, service: UserService = Depends(get_service)):
    return await service.search_by_name(name)


@router.get("/search/email", response_model=List[UserResponse])
async def search_by_email(email: str, service: UserService = Depends(get_service)):
    return await service.search_by_email(email)
