# Task 5 Report: Python — Pydantic request validation

## Status: DONE

## Summary

Replaced hand-rolled `Validator` classes in `create_user`, `register_bootstrap`, and `update_user` with Pydantic `Field` constraints and `field_validator` (strip name/email). Endpoints now rely on FastAPI request validation (422 on invalid input). Handler tests rewritten to assert `ValidationError` on request models.

## TDD Evidence

1. **Failing phase:** Added `test_create_user_request_rejects_short_name`, endpoint 422 assertions; 3 tests failed (model had no constraints, endpoints returned 400 Problem Details).
2. **Implementation:** Added `EmailStr`, `Field(min/max_length)`, `strip_strings` validator; removed `Validator`/`ValidationError` dataclasses and manual validation branches.
3. **Passing phase:** `uv run pytest tests/features/users -v --no-cov` → **87 passed**

## Commits

| SHA | Subject |
|-----|---------|
| `9f78376` | refactor: validate user commands via Pydantic request models |

## Files Modified

| Path |
|------|
| `python/src/features/users/create_user.py` |
| `python/src/features/users/register_bootstrap.py` |
| `python/src/features/users/update_user.py` |
| `python/tests/features/users/test_create_user_handler.py` |
| `python/tests/features/users/test_create_user_endpoint.py` |
| `python/tests/features/users/test_register_bootstrap_handler.py` |
| `python/tests/features/users/test_register_bootstrap_endpoint.py` |
| `python/tests/features/users/test_update_user_handler.py` |

## Concerns

1. **422 vs Problem Details:** Invalid user payloads now return FastAPI default 422 JSON (`detail` array), not `Validation.Error` Problem Details (400). Auth slices (`login`, `refresh_token`) still use hand-rolled validators — out of scope.
2. **Email validation shape:** `EmailStr` replaces custom regex; error messages differ from legacy strings but constraints are equivalent.
3. **`email-validator`:** Already present in `pyproject.toml` via `pydantic[email]` — no dependency change needed.

## Test Command

```bash
cd python && uv run pytest tests/features/users -v
```

**Result:** 87 passed.
