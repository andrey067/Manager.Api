from common.error import Error
from common.problem import problem_response


def test_problem_not_found_status():
    resp = problem_response(Error.not_found("Users.NotFound", "missing"))
    assert resp.status_code == 404
    assert resp.body  # JSON includes errorCode
