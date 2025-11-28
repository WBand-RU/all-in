import json
import redis
import tempfile
import requests
from pathlib import Path
from typing import List
from .schemas import MixTaskData, MixResult, MixTaskStatus
from .minio_client import MinIOClient
from .ffmpeg_mixer import FFMpegMixer
from .config import settings


class MixWorker:
    def __init__(self):
        self.redis_client = redis.from_url(settings.redis_url)
        self.minio_client = MinIOClient()
        self.mixer = FFMpegMixer()
        self.queue_name = "mix_tasks"

    def process_task(self, task_data: MixTaskData) -> MixResult:
        try:
            with tempfile.TemporaryDirectory() as temp_dir:
                temp_path = Path(temp_dir)
                
                # Скачиваем входные файлы из MinIO
                input_paths: List[Path] = []
                for i, object_name in enumerate(task_data.input_files):
                    local_path = temp_path / f"input_{i}_{Path(object_name).name}"
                    self.minio_client.download_file(object_name, local_path)
                    input_paths.append(local_path)

                # Определяем путь выходного файла
                output_path = temp_path / f"output.{task_data.format}"
                
                # Микшируем
                self.mixer.mix(
                    input_paths,
                    output_path,
                    task_data.format,
                    normalize=task_data.normalize,
                    target_lufs=task_data.target_lufs
                )

                # Загружаем результат в MinIO
                self.minio_client.upload_file(output_path, task_data.output_file)

                return MixResult(
                    task_id=task_data.task_id,
                    status=MixTaskStatus.COMPLETED,
                    output_file=task_data.output_file
                )

        except Exception as e:
            return MixResult(
                task_id=task_data.task_id,
                status=MixTaskStatus.FAILED,
                error_message=str(e)
            )

    def send_callback(self, result: MixResult, callback_url: str):
        try:
            requests.post(callback_url, json=result.dict(), timeout=10)
        except Exception as e:
            print(f"Failed to send callback: {e}")

    def run(self):
        print(f"Mixer worker started, listening to queue: {self.queue_name}")
        
        while True:
            try:
                # Блокирующее получение задачи из очереди
                result = self.redis_client.blpop(self.queue_name, timeout=1)
                if not result:
                    continue

                _, task_json = result
                task_dict = json.loads(task_json.decode())
                task_data = MixTaskData(**task_dict)
                
                print(f"Processing task: {task_data.task_id}")
                
                # Обрабатываем задачу
                result = self.process_task(task_data)
                
                # Отправляем результат через callback
                self.send_callback(result, task_data.callback_url)
                
                print(f"Task {task_data.task_id} completed with status: {result.status}")
                
            except KeyboardInterrupt:
                print("Worker stopped")
                break
            except Exception as e:
                print(f"Error processing task: {e}")
                continue
