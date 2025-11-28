import shutil
import subprocess
from pathlib import Path
from typing import List, Optional
from .config import settings


class FFMpegMixer:
    def __init__(self, ffmpeg_path: Optional[str] = None) -> None:
        self.ffmpeg = ffmpeg_path or settings.ffmpeg_path or shutil.which("ffmpeg")
        if not self.ffmpeg:
            raise Exception("ffmpeg не найден в PATH")

    def build_command(self, inputs: List[Path], output: Path, fmt: str, *, normalize: bool = False, target_lufs: float = -14.0) -> List[str]:
        if not inputs:
            raise Exception("Не указаны входные файлы")

        fmt = fmt.lower()
        codec_map = {
            "wav": ["-c:a", "pcm_s16le"],
            "mp3": ["-c:a", "libmp3lame", "-b:a", "320k"],
            "flac": ["-c:a", "flac"],
            "ogg": ["-c:a", "libvorbis", "-q:a", "6"],
            "aac": ["-c:a", "aac", "-b:a", "192k"],
        }
        if fmt not in codec_map:
            raise Exception(f"Неизвестный формат: {fmt}")

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

    def mix(self, inputs: List[Path], output: Path, fmt: str, *, normalize: bool = False, target_lufs: float = -14.0) -> None:
        for p in inputs:
            if not p.exists():
                raise Exception(f"Файл не найден: {p}")

        cmd = self.build_command(inputs, output, fmt, normalize=normalize, target_lufs=target_lufs)

        try:
            proc = subprocess.run(
                cmd,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                check=False,
            )
        except FileNotFoundError as e:
            raise Exception("ffmpeg не найден для запуска") from e

        if proc.returncode != 0:
            raise Exception(proc.stderr.strip() or "Неизвестная ошибка ffmpeg")
