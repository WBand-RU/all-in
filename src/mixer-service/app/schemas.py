from pydantic import BaseModel
from typing import List
from enum import Enum


class MixTaskStatus(str, Enum):
    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"


class MixTaskData(BaseModel):
    task_id: str
    input_files: List[str]  # MinIO object names
    output_file: str       # MinIO object name
    format: str
    normalize: bool = False
    target_lufs: float = -14.0
    callback_url: str      # URL для уведомления о завершении


class MixResult(BaseModel):
    task_id: str
    status: MixTaskStatus
    output_file: str = None
    error_message: str = None
