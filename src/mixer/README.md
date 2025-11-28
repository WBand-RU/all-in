# WBand Mixer (Desktop)

Простое десктоп‑приложение на Python (Tkinter) для микширования нескольких аудиофайлов c помощью внешней утилиты ffmpeg.

## Требования
- Windows
- Python 3.10+
- Виртуальное окружение уже создано: `src/mixer/venv/`
- Установленный ffmpeg и доступный в PATH (проверьте в PowerShell: `ffmpeg -version`).
  - Скачать: https://www.gyan.dev/ffmpeg/builds/ (download release full/shared)
  - Распакуйте и добавьте путь к `bin` в переменную окружения PATH.

## Запуск
```powershell
# Активировать venv
# Важно: путь может отличаться в зависимости от вашей среды
& .\src\mixer\venv\Scripts\Activate.ps1

# Запустить приложение
python .\src\mixer\main.py
```

## Использование
- Кнопка `+` — добавить входные аудиофайлы.
- Кнопка `×` — удалить выбранный в списке файл.
- `Формат` — выбрать формат выходного файла (wav, mp3, flac, ogg, aac).
- `...` — выбрать путь и имя выходного файла.
- `MIX` — начать микширование. Используется фильтр `amix`.

## Архитектура
- `app/core/ffmpeg_mixer.py` — сервис для вызова ffmpeg (SOLID: единая ответственность).
- `app/ui/app.py` — минимальный GUI на Tkinter.
- `main.py` — точка входа.

## Примечания
- Для WAV используется кодек `pcm_s16le`.
- Для MP3 — `libmp3lame` (требуется сборка ffmpeg с этим кодеком).
- Для FLAC — `flac`.
- Для OGG — `libvorbis`.
- Для AAC — `aac`.
