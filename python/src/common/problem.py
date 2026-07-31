from __future__ import annotations

from fastapi.responses import JSONResponse

from common.error import Error, ErrorType

_STATUS_BY_TYPE: dict[ErrorType, int] = {
    ErrorType.NOT_FOUND: 404,
    ErrorType.CONFLICT: 409,
    ErrorType.VALIDATION: 400,
    ErrorType.PROBLEM: 400,
    ErrorType.FAILURE: 500,
}

_TYPE_URI_BY_ERROR_TYPE: dict[ErrorType, str] = {
    ErrorType.NOT_FOUND: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
    ErrorType.CONFLICT: "https://tools.ietf.org/html/rfc7231#section-6.5.8",
    ErrorType.VALIDATION: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    ErrorType.PROBLEM: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    ErrorType.FAILURE: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
}


def _map_status_code(error_type: ErrorType) -> int:
    return _STATUS_BY_TYPE.get(error_type, 500)


def problem_response(error: Error) -> JSONResponse:
    status_code = _map_status_code(error.type)
    body = {
        "type": _TYPE_URI_BY_ERROR_TYPE.get(error.type, "about:blank"),
        "title": error.type.value,
        "status": status_code,
        "detail": error.description,
        "errorCode": error.code,
    }
    return JSONResponse(
        content=body,
        status_code=status_code,
        media_type="application/problem+json",
    )
