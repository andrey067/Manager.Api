from __future__ import annotations

from typing import Any


class Entity:
    def __init__(self) -> None:
        self.id: int = 0
        self._domain_events: list[Any] = []

    def raise_event(self, event: Any) -> None:
        self._domain_events.append(event)

    @property
    def domain_events(self) -> tuple[Any, ...]:
        return tuple(self._domain_events)

    def clear_domain_events(self) -> None:
        self._domain_events.clear()
