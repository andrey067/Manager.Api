from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class UserCreatedDomainEvent:
    id: int


@dataclass(frozen=True, slots=True)
class UserUpdatedDomainEvent:
    id: int


@dataclass(frozen=True, slots=True)
class UserRemovedDomainEvent:
    id: int
