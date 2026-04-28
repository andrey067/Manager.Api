"""Core error types."""


class DomainError(Exception):
    """Raised when a domain entity is invalid."""
    def __init__(self, errors: list[str]):
        self.errors = errors
        super().__init__("; ".join(errors))


class NotFoundError(Exception):
    """Raised when a requested resource does not exist."""
    def __init__(self, message: str = "Resource not found"):
        super().__init__(message)


class ConflictError(Exception):
    """Raised when a uniqueness constraint is violated."""
    def __init__(self, message: str = "Resource already exists"):
        super().__init__(message)
