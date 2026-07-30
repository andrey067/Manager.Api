from dataclasses import dataclass

from common.entity import Entity


@dataclass(frozen=True)
class _Evt:
    id: int


def test_raise_and_clear():
    e = Entity()
    e.raise_event(_Evt(1))
    assert len(e.domain_events) == 1
    e.clear_domain_events()
    assert e.domain_events == ()
