"""Application-layer DTOs (Data Transfer Objects)."""
from dataclasses import dataclass
from typing import Optional


@dataclass
class CreateUserDTO:
    name: str
    email: str
    password: str


@dataclass
class UpdateUserDTO:
    id: int
    name: str
    email: str
    password: str


@dataclass
class UserDTO:
    id: Optional[int]
    name: str
    email: str
    # password intentionally omitted from output DTO
