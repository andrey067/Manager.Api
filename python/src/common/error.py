from dataclasses import dataclass
from enum import Enum


class ErrorType(Enum):
    FAILURE = "Failure"
    VALIDATION = "Validation"
    NOT_FOUND = "NotFound"
    CONFLICT = "Conflict"
    PROBLEM = "Problem"

@dataclass(frozen=True, slots=True)
class Error:
    code: str
    description: str
    type: ErrorType

    @staticmethod
    def not_found(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.NOT_FOUND)

    @staticmethod
    def conflict(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.CONFLICT)

    @staticmethod
    def validation(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.VALIDATION)

    @staticmethod
    def failure(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.FAILURE)

    @staticmethod
    def problem(code: str, description: str) -> "Error":
        return Error(code, description, ErrorType.PROBLEM)
