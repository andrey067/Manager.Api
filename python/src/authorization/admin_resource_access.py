from __future__ import annotations

_ADMIN_MANAGED = frozenset({"Users"})


def is_admin_managed_feature(feature_name: str) -> bool:
    return feature_name in _ADMIN_MANAGED


def requires_resource_ownership(feature_name: str) -> bool:
    return feature_name not in _ADMIN_MANAGED
