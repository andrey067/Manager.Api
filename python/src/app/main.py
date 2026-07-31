"""VSA FastAPI application entrypoint."""

from fastapi import FastAPI

from app.registry import register_routes

app = FastAPI(title="Manager VSA API", version="0.1.0")
register_routes(app)


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}
