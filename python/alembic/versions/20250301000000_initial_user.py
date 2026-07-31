"""Initial User table with unique email.

Revision ID: 20250301000000
Revises:
Create Date: 2025-03-01 00:00:00
"""

from collections.abc import Sequence

import sqlalchemy as sa
from alembic import op

revision: str = "20250301000000"
down_revision: str | None = None
branch_labels: str | Sequence[str] | None = None
depends_on: str | Sequence[str] | None = None


def upgrade() -> None:
    op.create_table(
        "User",
        sa.Column("id", sa.Integer(), autoincrement=True, nullable=False),
        sa.Column("name", sa.String(length=80), nullable=False),
        sa.Column("email", sa.String(length=180), nullable=False),
        sa.Column("password", sa.String(length=1000), nullable=False),
        sa.PrimaryKeyConstraint("id"),
        sa.UniqueConstraint("email", name="IX_User_Email"),
    )


def downgrade() -> None:
    op.drop_table("User")
