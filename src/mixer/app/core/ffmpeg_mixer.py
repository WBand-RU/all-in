import shutil
import subprocess
from pathlib import Path
from typing import Iterable, List, Optional

from .exceptions import FFMpegNotFoundError, MixingError


class FFMpegMixer:
    def __init__(self, ffmpeg_path: Optional[str] = None) -> None:
        self.ffmpeg = ffmpeg_path or shutil.which("ffmpeg")
        if not self.ffmpeg:
            raise FFMpegNotFoundError("ffmpeg не найден в PATH. Установите ffmpeg и добавьте в PATH.")

    def build_command(self, inputs: List[Path], output: Path, fmt: str, *, normalize: bool = False, target_lufs: float = -14.0) -> List[str]:
        if not inputs:
            raise MixingError("Не указаны входные файлы")

        # Подбор кодека по формату
        fmt = fmt.lower()
        codec_map = {
            "wav": ["-c:a", "pcm_s16le"],
            "mp3": ["-c:a", "libmp3lame", "-b:a", "320k"],
            "flac": ["-c:a", "flac"],
            "ogg": ["-c:a", "libvorbis", "-q:a", "6"],
            "aac": ["-c:a", "aac", "-b:a", "192k"],
        }
        if fmt not in codec_map:
            raise MixingError(f"Неизвестный формат: {fmt}")

        cmd: List[str] = [self.ffmpeg, "-y"]
        for p in inputs:
            cmd += ["-i", str(p)]

        amix = f"amix=inputs={len(inputs)}:normalize=0"
        if normalize:
            loudnorm = f"loudnorm=I={target_lufs}:TP=-1.5:LRA=11"
            filter_complex = f"{amix},{loudnorm}"
        else:
            filter_complex = amix

        cmd += ["-filter_complex", filter_complex]
        cmd += codec_map[fmt]
        cmd += [str(output)]
        return cmd

    def mix(self, inputs: Iterable[str | Path], output: str | Path, fmt: str, *, normalize: bool = False, target_lufs: float = -14.0) -> None:
        input_paths = [Path(p) for p in inputs]
        output_path = Path(output)

        for p in input_paths:
            if not p.exists():
                raise MixingError(f"Файл не найден: {p}")

        if output_path.suffix.lower().lstrip(".") != fmt.lower():
            output_path = output_path.with_suffix(f".{fmt}")

        cmd = self.build_command(input_paths, output_path, fmt, normalize=normalize, target_lufs=target_lufs)

        try:
            proc = subprocess.run(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                check=False,
            )
        except FileNotFoundError as e:
            raise FFMpegNotFoundError("ffmpeg не найден для запуска") from e

        if proc.returncode != 0:
            raise MixingError(proc.stderr.strip() or "Неизвестная ошибка ffmpeg")

