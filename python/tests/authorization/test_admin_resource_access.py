from authorization.admin_resource_access import (
    is_admin_managed_feature,
    requires_resource_ownership,
)


def test_users_is_admin_managed() -> None:
    assert is_admin_managed_feature("Users") is True
    assert requires_resource_ownership("Users") is False


def test_unknown_requires_ownership() -> None:
    assert requires_resource_ownership("Orders") is True
