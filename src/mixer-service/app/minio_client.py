from minio import Minio
from minio.error import S3Error
from pathlib import Path
from typing import List
import tempfile
from .config import settings


class MinIOClient:
    def __init__(self):
        self.client = Minio(
            settings.minio_endpoint,
            access_key=settings.minio_access_key,
            secret_key=settings.minio_secret_key,
            secure=settings.minio_secure
        )
        self._ensure_bucket()

    def _ensure_bucket(self):
        if not self.client.bucket_exists(settings.bucket_name):
            self.client.make_bucket(settings.bucket_name)

    def download_file(self, object_name: str, local_path: Path) -> None:
        try:
            self.client.fget_object(settings.bucket_name, object_name, str(local_path))
        except S3Error as e:
            raise Exception(f"Failed to download {object_name}: {e}")

    def upload_file(self, local_path: Path, object_name: str) -> None:
        try:
            self.client.fput_object(settings.bucket_name, object_name, str(local_path))
        except S3Error as e:
            raise Exception(f"Failed to upload {object_name}: {e}")

    def generate_presigned_upload_url(self, object_name: str, expires_seconds: int = 3600) -> str:
        try:
            return self.client.presigned_put_object(settings.bucket_name, object_name, expires=expires_seconds)
        except S3Error as e:
            raise Exception(f"Failed to generate upload URL for {object_name}: {e}")

    def generate_presigned_download_url(self, object_name: str, expires_seconds: int = 3600) -> str:
        try:
            return self.client.presigned_get_object(settings.bucket_name, object_name, expires=expires_seconds)
        except S3Error as e:
            raise Exception(f"Failed to generate download URL for {object_name}: {e}")
