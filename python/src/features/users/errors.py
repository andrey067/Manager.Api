from __future__ import annotations

from common.error import Error


class UserErrors:
    @staticmethod
    def not_found() -> Error:
        return Error.not_found(
            "Users.NotFound",
            "Não existe nenhum usuário com o id informado.",
        )

    @staticmethod
    def email_conflict() -> Error:
        return Error.conflict(
            "Users.EmailConflict",
            "Já existe um usuário cadastrado com o email informado.",
        )

    @staticmethod
    def bootstrap_not_allowed() -> Error:
        return Error.problem(
            "Users.BootstrapNotAllowed",
            "O registro inicial só é permitido quando não existem usuários cadastrados.",
        )
