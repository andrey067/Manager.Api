"""
TDD - Application/service layer unit tests.
Written BEFORE the service implementation.
"""
import pytest
from unittest.mock import MagicMock, AsyncMock
from src.domain.entities import User
from src.application.dtos import CreateUserDTO, UpdateUserDTO, UserDTO
from src.application.interfaces import IUserRepository
from src.application.services import UserService
from src.core.errors import DomainError, NotFoundError, ConflictError


def make_user(id=1, name="John Doe", email="john@example.com", password="secret123"):
    u = User(name=name, email=email, password=password)
    u.id = id
    return u


class TestUserServiceCreate:
    @pytest.fixture
    def repo(self):
        r = AsyncMock(spec=IUserRepository)
        r.get_by_email.return_value = None  # no existing user
        return r

    @pytest.fixture
    def service(self, repo):
        return UserService(repo)

    @pytest.mark.asyncio
    async def test_create_returns_dto(self, service, repo):
        saved = make_user()
        repo.create.return_value = saved
        dto = CreateUserDTO(name="John Doe", email="john@example.com", password="secret123")
        result = await service.create(dto)
        assert result.email == "john@example.com"
        assert result.name == "John Doe"

    @pytest.mark.asyncio
    async def test_create_raises_conflict_if_email_taken(self, service, repo):
        repo.get_by_email.return_value = make_user()
        dto = CreateUserDTO(name="John Doe", email="john@example.com", password="secret123")
        with pytest.raises(ConflictError):
            await service.create(dto)

    @pytest.mark.asyncio
    async def test_create_raises_domain_error_for_invalid_entity(self, service, repo):
        dto = CreateUserDTO(name="Jo", email="john@example.com", password="secret123")
        with pytest.raises(DomainError):
            await service.create(dto)


class TestUserServiceUpdate:
    @pytest.fixture
    def repo(self):
        r = AsyncMock(spec=IUserRepository)
        existing = make_user()
        r.get_by_id.return_value = existing
        r.get_by_email.return_value = None
        r.update.return_value = make_user(name="Jane Doe")
        return r

    @pytest.fixture
    def service(self, repo):
        return UserService(repo)

    @pytest.mark.asyncio
    async def test_update_returns_updated_dto(self, service, repo):
        dto = UpdateUserDTO(id=1, name="Jane Doe", email="john@example.com", password="secret123")
        result = await service.update(dto)
        assert result.name == "Jane Doe"

    @pytest.mark.asyncio
    async def test_update_raises_not_found_for_missing_user(self, service, repo):
        repo.get_by_id.return_value = None
        dto = UpdateUserDTO(id=999, name="Jane Doe", email="john@example.com", password="secret123")
        with pytest.raises(NotFoundError):
            await service.update(dto)


class TestUserServiceGet:
    @pytest.fixture
    def repo(self):
        r = AsyncMock(spec=IUserRepository)
        r.get_by_id.return_value = make_user()
        r.get_all.return_value = [make_user(), make_user(id=2, email="other@example.com")]
        r.search_by_name.return_value = [make_user()]
        r.search_by_email.return_value = [make_user()]
        r.get_by_email.return_value = make_user()
        return r

    @pytest.fixture
    def service(self, repo):
        return UserService(repo)

    @pytest.mark.asyncio
    async def test_get_by_id_returns_dto(self, service, repo):
        result = await service.get_by_id(1)
        assert result.id == 1

    @pytest.mark.asyncio
    async def test_get_by_id_raises_not_found(self, service, repo):
        repo.get_by_id.return_value = None
        with pytest.raises(NotFoundError):
            await service.get_by_id(999)

    @pytest.mark.asyncio
    async def test_get_all_returns_list(self, service, repo):
        result = await service.get_all()
        assert len(result) == 2

    @pytest.mark.asyncio
    async def test_search_by_name(self, service, repo):
        result = await service.search_by_name("John")
        assert len(result) == 1

    @pytest.mark.asyncio
    async def test_search_by_email(self, service, repo):
        result = await service.search_by_email("john")
        assert len(result) == 1

    @pytest.mark.asyncio
    async def test_get_by_email_returns_dto(self, service, repo):
        result = await service.get_by_email("john@example.com")
        assert result.email == "john@example.com"


class TestUserServiceDelete:
    @pytest.fixture
    def repo(self):
        r = AsyncMock(spec=IUserRepository)
        r.get_by_id.return_value = make_user()
        r.delete.return_value = None
        return r

    @pytest.fixture
    def service(self, repo):
        return UserService(repo)

    @pytest.mark.asyncio
    async def test_delete_calls_repository(self, service, repo):
        await service.delete(1)
        repo.delete.assert_called_once_with(1)

    @pytest.mark.asyncio
    async def test_delete_raises_not_found(self, service, repo):
        repo.get_by_id.return_value = None
        with pytest.raises(NotFoundError):
            await service.delete(999)
