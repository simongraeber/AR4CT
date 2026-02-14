"""FastAPI application factory."""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.config import CORS_ORIGINS
from app.routes import all_routers


def create_app() -> FastAPI:
    application = FastAPI(title="AR4CT API", version="1.0.0")

    application.add_middleware(
        CORSMiddleware,
        allow_origins=CORS_ORIGINS,
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    @application.get("/")
    async def root():
        return {"message": "AR4CT API", "version": "1.0.0"}

    for router in all_routers:
        application.include_router(router)

    return application


app = create_app()
