from __future__ import annotations
from collections.abc import Callable
from typing import Generic, TypeVar
from common.error import Error

T = TypeVar("T")
R = TypeVar("R")

class Result(Generic[T]):
    def __init__(self, value: T | None, error: Error | None, is_success: bool) -> None:
        self._value = value
        self._error = error
        self.is_success = is_success

    @property
    def is_failure(self) -> bool:
        return not self.is_success

    @property
    def error(self) -> Error:
        if self._error is None:
            raise RuntimeError("No error on success")
        return self._error

    @property
    def value(self) -> T:
        if not self.is_success:
            raise RuntimeError("No value on failure")
        return self._value  # type: ignore[return-value]

    @staticmethod
    def success(value: T | None = None) -> Result[T]:
        return Result(value, None, True)

    @staticmethod
    def failure(error: Error) -> Result[T]:
        return Result(None, error, False)

    def match(self, on_success: Callable[..., R], on_failure: Callable[[Error], R]) -> R:
        if self.is_success:
            if self._value is None:
                return on_success()
            return on_success(self._value)
        return on_failure(self.error)
