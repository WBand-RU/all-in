from pydantic_settings import BaseSettings
from typing import Optional


class Settings(BaseSettings):
    redis_url: str = "redis://localhost:6379"
    minio_endpoint: str = "localhost:9000"
    minio_access_key: str = "minioadmin"
    minio_secret_key: str = "minioadmin123"
    minio_secure: bool = False
    bucket_name: str = "wband-files"
    ffmpeg_path: Optional[str] = None

    class Config:
        env_file = ".env"


settings = Settings()
