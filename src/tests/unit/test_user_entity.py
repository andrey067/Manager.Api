"""
TDD - Domain layer unit tests.
These tests define the expected behavior BEFORE implementation.
"""
import pytest
from src.domain.entities import User
from src.domain.validators import UserValidator, ValidationError


class TestUserEntity:
    """Tests for User entity - written BEFORE implementation."""

    def test_create_valid_user(self):
        user = User(name="John Doe", email="john@example.com", password="secret123")
        assert user.name == "John Doe"
        assert user.email == "john@example.com"
        assert user.password == "secret123"
        assert user.is_valid()

    def test_user_id_is_none_before_persist(self):
        user = User(name="John Doe", email="john@example.com", password="secret123")
        assert user.id is None

    # --- Name validation ---

    def test_name_cannot_be_empty(self):
        user = User(name="", email="john@example.com", password="secret123")
        assert not user.is_valid()
        assert any("nome" in e.lower() for e in user.errors)

    def test_name_cannot_be_none(self):
        user = User(name=None, email="john@example.com", password="secret123")
        assert not user.is_valid()

    def test_name_min_length_3(self):
        user = User(name="Jo", email="john@example.com", password="secret123")
        assert not user.is_valid()

    def test_name_max_length_80(self):
        user = User(name="A" * 81, email="john@example.com", password="secret123")
        assert not user.is_valid()

    def test_name_exactly_3_chars_is_valid(self):
        user = User(name="Joe", email="joe@example.com", password="secret123")
        assert user.is_valid()

    def test_name_exactly_80_chars_is_valid(self):
        user = User(name="A" * 80, email="aaa@example.com", password="secret123")
        assert user.is_valid()

    # --- Email validation ---

    def test_email_cannot_be_empty(self):
        user = User(name="John Doe", email="", password="secret123")
        assert not user.is_valid()

    def test_email_must_be_valid_format(self):
        user = User(name="John Doe", email="not-an-email", password="secret123")
        assert not user.is_valid()

    def test_email_min_length_10(self):
        user = User(name="John Doe", email="a@b.c", password="secret123")
        assert not user.is_valid()

    def test_email_max_length_180(self):
        long_email = "a" * 175 + "@b.com"
        user = User(name="John Doe", email=long_email, password="secret123")
        assert not user.is_valid()

    def test_valid_email_format(self):
        user = User(name="John Doe", email="john.doe@example.com", password="secret123")
        assert user.is_valid()

    # --- Password validation ---

    def test_password_cannot_be_empty(self):
        user = User(name="John Doe", email="john@example.com", password="")
        assert not user.is_valid()

    def test_password_min_length_6(self):
        user = User(name="John Doe", email="john@example.com", password="abc")
        assert not user.is_valid()

    def test_password_max_length_80(self):
        user = User(name="John Doe", email="john@example.com", password="a" * 81)
        assert not user.is_valid()

    def test_password_exactly_6_chars_is_valid(self):
        user = User(name="John Doe", email="john@example.com", password="abc123")
        assert user.is_valid()

    # --- Mutators ---

    def test_set_name_updates_and_revalidates(self):
        user = User(name="John Doe", email="john@example.com", password="secret123")
        user.set_name("Jane Doe")
        assert user.name == "Jane Doe"
        assert user.is_valid()

    def test_set_name_to_invalid_marks_invalid(self):
        user = User(name="John Doe", email="john@example.com", password="secret123")
        user.set_name("Jo")
        assert not user.is_valid()

    def test_set_email_updates_and_revalidates(self):
        user = User(name="John Doe", email="john@example.com", password="secret123")
        user.set_email("new@example.com")
        assert user.email == "new@example.com"
        assert user.is_valid()

    def test_set_password_updates_and_revalidates(self):
        user = User(name="John Doe", email="john@example.com", password="secret123")
        user.set_password("newpass456")
        assert user.password == "newpass456"
        assert user.is_valid()

    def test_errors_cleared_on_revalidation(self):
        user = User(name="Jo", email="john@example.com", password="secret123")
        assert not user.is_valid()
        assert len(user.errors) > 0
        user.set_name("John Doe")
        assert user.is_valid()
        assert len(user.errors) == 0
