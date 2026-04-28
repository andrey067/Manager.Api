"""Domain entities."""
from typing import Optional, List
from .validators import UserValidator


class Base:
    def __init__(self):
        self.id: Optional[int] = None
        self._errors: List[str] = []

    @property
    def errors(self) -> List[str]:
        return self._errors

    def is_valid(self) -> bool:
        return len(self._errors) == 0


class User(Base):
    """User domain entity.
    
    Self-validates on creation and mutation — mirrors the .NET implementation.
    """

    def __init__(self, name: Optional[str], email: Optional[str], password: Optional[str]):
        super().__init__()
        self._name = name
        self._email = email
        self._password = password
        self._validate()

    # --- Properties (read-only outside mutators) ---

    @property
    def name(self) -> Optional[str]:
        return self._name

    @property
    def email(self) -> Optional[str]:
        return self._email

    @property
    def password(self) -> Optional[str]:
        return self._password

    # --- Mutators ---

    def set_name(self, name: str) -> None:
        self._name = name
        self._validate()

    def set_email(self, email: str) -> None:
        self._email = email
        self._validate()

    def set_password(self, password: str) -> None:
        self._password = password
        self._validate()

    # --- Internal ---

    def _validate(self) -> bool:
        validator = UserValidator()
        valid = validator.validate(self)
        self._errors = validator.errors
        return valid
