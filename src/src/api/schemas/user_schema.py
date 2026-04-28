"""Pydantic schemas for API request/response."""
from pydantic import BaseModel, EmailStr, Field
from typing import Optional


class CreateUserRequest(BaseModel):
    name: str = Field(..., min_length=3, max_length=80, description="User full name")
    email: EmailStr = Field(..., description="User email address")
    password: str = Field(..., min_length=6, max_length=80, description="User password")


class UpdateUserRequest(BaseModel):
    name: str = Field(..., min_length=3, max_length=80)
    email: EmailStr
    password: str = Field(..., min_length=6, max_length=80)


class UserResponse(BaseModel):
    id: Optional[int]
    name: str
    email: str

    model_config = {"from_attributes": True}


class ErrorResponse(BaseModel):
    detail: str
    errors: Optional[list[str]] = None
