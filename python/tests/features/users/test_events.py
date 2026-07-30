from __future__ import annotations

from features.users.events import (
    UserCreatedDomainEvent,
    UserRemovedDomainEvent,
    UserUpdatedDomainEvent,
)


def test_user_created_domain_event_id() -> None:
    assert UserCreatedDomainEvent(id=7).id == 7


def test_user_updated_domain_event_id() -> None:
    assert UserUpdatedDomainEvent(id=8).id == 8


def test_user_removed_domain_event_id() -> None:
    assert UserRemovedDomainEvent(id=9).id == 9
