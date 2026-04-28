"""Domain validators for User entity."""
import re
from dataclasses import dataclass, field
from typing import List


class ValidationError(Exception):
    pass


@dataclass
class UserValidator:
    errors: List[str] = field(default_factory=list)

    def validate(self, user) -> bool:
        self.errors = []

        # Name
        if user.name is None:
            self.errors.append("O nome não pode ser nulo.")
        elif user.name == "":
            self.errors.append("O nome não pode ser vazio.")
        elif len(user.name) < 3:
            self.errors.append("O nome deve ter no mínimo 3 caracteres.")
        elif len(user.name) > 80:
            self.errors.append("O nome deve ter no máximo 80 caracteres.")

        # Email
        if user.email is None:
            self.errors.append("O email não pode ser nulo.")
        elif user.email == "":
            self.errors.append("O email não pode ser vazio.")
        elif len(user.email) < 10:
            self.errors.append("O email deve ter no mínimo 10 caracteres.")
        elif len(user.email) > 180:
            self.errors.append("O email deve ter no máximo 180 caracteres.")
        else:
            pattern = r'^([\w\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$'
            if not re.match(pattern, user.email):
                self.errors.append("O email informado não é válido.")

        # Password
        if user.password is None:
            self.errors.append("A senha não pode ser nula.")
        elif user.password == "":
            self.errors.append("A senha não pode ser vazia.")
        elif len(user.password) < 6:
            self.errors.append("A senha deve ter no mínimo 6 caracteres.")
        elif len(user.password) > 80:
            self.errors.append("A senha deve ter no máximo 80 caracteres.")

        return len(self.errors) == 0
