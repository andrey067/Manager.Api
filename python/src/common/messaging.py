from __future__ import annotations

from typing import Protocol, TypeVar

from common.result import Result

TCommand = TypeVar("TCommand")
TQuery = TypeVar("TQuery")
TResponse = TypeVar("TResponse")


class CommandHandler(Protocol[TCommand]):
    async def handle(self, command: TCommand) -> Result[None]:
        ...


class CommandHandlerWithResponse(Protocol[TCommand, TResponse]):
    async def handle(self, command: TCommand) -> Result[TResponse]:
        ...


class QueryHandler(Protocol[TQuery, TResponse]):
    async def handle(self, query: TQuery) -> Result[TResponse]:
        ...
