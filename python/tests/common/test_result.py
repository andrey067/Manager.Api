from common.error import Error, ErrorType
from common.result import Result


def test_success_match():
    result = Result.success()
    assert result.is_success
    assert result.match(lambda: 1, lambda e: 0) == 1


def test_failure_match():
    err = Error.not_found("Users.NotFound", "User not found")
    result = Result.failure(err)
    assert not result.is_success
    assert result.error.code == "Users.NotFound"
    assert result.error.type == ErrorType.NOT_FOUND
    assert result.match(lambda: 1, lambda e: 0) == 0


def test_result_t_value():
    result = Result.success(42)
    assert result.value == 42
    assert result.match(lambda v: v, lambda e: -1) == 42
