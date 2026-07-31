"""Add refresh token columns to User table.

Revision ID: 20260730234500
Revises: 20250301000000
Create Date: 2026-07-30 23:45:00
"""

from collections.abc import Sequence

import sqlalchemy as sa
from alembic import op

revision: str = "20260730234500"
down_revision: str | None = "20250301000000"
branch_labels: str | Sequence[str] | None = None
depends_on: str | Sequence[str] | None = None


def upgrade() -> None:
    op.add_column(
        "User",
        sa.Column("refresh_token_hash", sa.String(length=128), nullable=True),
    )
    op.add_column(
        "User",
        sa.Column(
            "refresh_token_expires_at",
            sa.DateTime(timezone=True),
            nullable=True,
        ),
    )


def downgrade() -> None:
    op.drop_column("User", "refresh_token_expires_at")
    op.drop_column("User", "refresh_token_hash")
