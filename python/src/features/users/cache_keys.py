from __future__ import annotations


class UserCacheKeys:
    ALL = "users:all"

    @staticmethod
    def by_id(id: int) -> str:
        return f"users:{id}"

    @staticmethod
    def by_email(email: str) -> str:
        return f"users:email:{email.lower()}"
