from __future__ import annotations

from features.users.events import (
    UserCreatedDomainEvent,
    UserLoggedInDomainEvent,
    UserRemovedDomainEvent,
    UserTokenRefreshedDomainEvent,
    UserUpdatedDomainEvent,
)


def test_user_created_domain_event_id() -> None:
    assert UserCreatedDomainEvent(id=7).id == 7


def test_user_updated_domain_event_id() -> None:
    assert UserUpdatedDomainEvent(id=8).id == 8


def test_user_removed_domain_event_id() -> None:
    assert UserRemovedDomainEvent(id=9).id == 9


def test_user_logged_in_domain_event_id() -> None:
    assert UserLoggedInDomainEvent(id=11).id == 11


def test_user_token_refreshed_domain_event_id() -> None:
    assert UserTokenRefreshedDomainEvent(id=12).id == 12
