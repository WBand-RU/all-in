# WBAND: Единый сервис репертуара и плейбеков

**Версия:** 1.0 (MVP + дорожная карта)  
**Язык интерфейса:** RU/EN  
**Домены окружений:** `test.wband.ru` (stage), `wband.ru` (prod)

> Документ описывает функционал, пользовательские сценарии, архитектуру (включая микросервисы и оффлайн-плеер), технические спецификации (языки/библиотеки/фреймворки), протоколы, API, модели данных, политику лицензий и дорожную карту.

---

## 1. Цели и обзор

Сервис уровня multitracks + bandfix: управление песнями и плейлистами, публикация в общий каталог, воспроизведение многодорожечных плейбеков, Live-режим, экспорт миксов и листов, оффлайн-плеер для сцены.

**Ключевые особенности**

-   Песни принадлежат бэнду, публикация/шаринг настраиваемы.
-   Структуры песен в **тактах** с поддержкой смен размера/темпа; произвольные названия секций.
-   Плейлисты «на событие»: своя тональность/темп/структура/уровни стемов.
-   Плеер: solo/mute, уровни/панорама, луп секции, прыжки, хоткеи/MIDI, предпросмотр.
-   Live Master Control (LAN в MVP), веб-клиенты-зрители с синхронным текстом/аккордами.
-   Оффлайн-кэш для «следующего события», автопересборка при изменениях.
-   Импорт (TXT/MD, DOCX, PDF OCR), экспорт PDF (листы и «книга плейлиста»).
-   Версионирование песен/плейлистов, корзина 30 дней.

---

## 2. Роли и разрешения

Роли настраиваемы **внутри бэнда**. Предустановки:

-   **Owner** — все права + удаление бэнда, выпуск/отзыв API-токенов.
-   **Admin** — все права (кроме удаления бэнда).
-   **Editor** — просмотр, редактирование песен/плейбеков, редактирование плейлистов, экспорт.
-   **Player (LM)** — просмотр + Live Master Control.
-   **Viewer** — только просмотр.
-   **Publisher** — публикация/шаринг + редактирование песен.

Разрешения (скоупы):

1. Просмотр
2. Редактирование ролей
3. Редактирование песен/плейбеков
4. Редактирование плейлистов
5. Публикация/шаринг в каталог
6. Управление участниками
7. Удаление сущностей
8. Экспорт/рендер миксов
9. Live Master Control

Приглашения: одноразовая ссылка (TTL 7 дней), после принятия/отклонения — инвалидируется.

---

## 3. Пользовательские сценарии (User Stories)

### 3.1 Онбординг и роли

-   Как **Owner**, я создаю бэнд, приглашаю участников, назначаю роли.
-   Как **Publisher**, я публикую/делюсь песней в каталог для других бэндов.
-   Как **Viewer**, я подключаюсь к Live-сессии под своим аккаунтом и вижу текст/аккорды.

### 3.2 Песни и редактор

-   Как **Editor**, я создаю песню, задаю тональность/BPM/размер, размечаю по тактам секции (Verse/Chorus/Bridge/Tag/Outro и произвольные).
-   Я редактирую текст с аккордами, использую автодополнение, транспонирование и каподастр (гитарные формы).

### 3.3 Импорт/экспорт

-   Я импортирую песню из TXT/Markdown/DOCX/PDF (OCR ru+en), автоматически распознаются аккорды и сетка тактов, я подтверждаю правки в редакторе.
-   Я экспортирую лист песни или «книгу плейлиста» в PDF (A4, портрет, светлая тема; шапка с тональностью/капо; обложка с логотипом бэнда).

### 3.4 Плейлисты и события

-   Как **Editor**, я собираю плейлист на выступление: порядок треков, паузы, переходы (автостарт/пауза N сек/кроссфейд).
-   Для каждой песни в плейлисте я задаю **свою** тональность, темп, структуру (не ломая оригинал), уровни/панораму стемов.

### 3.5 Плейбэки и плеер

-   Я загружаю WAV-стемы, объединяю их в группы (гитары, синты и т. п.).
-   В плеере я управляю solo/mute, уровнями, панорамой; прыгаю по секциям; ставлю луп; пользуюсь хоткеями/MIDI.
-   В Live я, как **Player (LM)**, управляю воспроизведением, а участники видят синхронный текст/аккорды.

### 3.6 Автокэш и готовность к выступлению

-   Серверный агент сразу рендерит и кэширует «следующее событие» и пересобирает при изменениях.
-   Desktop-плеер скачивает актуальный кэш; срок хранения задаёт пользователь (по умолчанию 7 дней).

### 3.7 Публикация и каталог

-   Владелец песен решает: «только бэнд», «по ссылке», «публично в каталоге».
-   Копирование из каталога создаёт **независимую копию** в другом бэнде (без связей с оригиналом).

### 3.8 Удаление и версии

-   Любую сущность можно отправить в «Корзину» на 30 дней; восстановить или удалить окончательно.
-   Песни/плейлисты имеют историю версий с откатом; правки блокируются на время редактирования.

---

## 4. Функциональные требования

### 4.1 Песни

-   Поля: название, автор(ы), тональность, BPM, размер; нотация аккордов **C D E F G A B** (без H).
-   Структура: секции с произвольными названиями (есть преднаборы), **диапазоны в тактах**.
-   Смены размера/темпа внутри песни (напр. 4/4 → 6/8; одиночные 2/4).
-   Count-in настраиваемый (по умолчанию 2 такта).
-   Автопривязка маркеров к сетке тактов при импорте аудио.
-   Вложения: документы, аудио, видео, ссылки.

### 4.2 Плейлисты

-   Параметры события: дата/время, место, порядок треков, перерывы.
-   На уровне песни в плейлисте: **своя** тональность, темп, структура, уровни/панорама стемов.
-   Переходы: автостарт/пауза N сек/кроссфейд (настраивается при сборке).

### 4.3 Плеер (web + desktop)

-   Управление стемами: solo/mute, уровни, панорама; клип-индикаторы.
-   Транспорт: маркеры секций, луп секции, переход к секции, count-in.
-   Pitch/tempo: ±12 полутонов, изменение темпа без смены высоты.
-   Метроклик/гид: настраиваемые сэмплы клика; **voice-guide (TTS)** по секциям, пол голоса на выбор.
-   Отображение: текущая/следующая секция, предпросмотр следующей песни, автоскролл текста/аккордов.
-   Управление: горячие клавиши, MIDI/футсвитч.

### 4.4 Live-синхронизация

-   Режим мастер–слейв: LAN автообнаружение (mDNS) + ручной ввод хоста/порта.
-   Через интернет (WebSocket) — **после MVP**.
-   Целевые задержки: LAN ≤ 20 мс, интернет ≤ 100 мс; периодический ресинк по тактовой сетке; джиттер-буфер включён.
-   Веб-клиенты-зрители подключаются по аккаунту (роль Viewer), **только просмотр**, лимитов нет.

### 4.5 Импорт/экспорт

-   Импорт: TXT/Markdown, DOCX, PDF (OCR ru+en). Распознавание аккордов и сетки тактов; быстрый флоу ручной правки.
-   Экспорт аудио: WAV 44.1 кГц 24-бит, MP3 320 kbps; нормализация/лимитер настраиваемы; дезеринг — только при 16-бит.
-   Экспорт PDF: отдельные песни и «книга плейлиста» (A4, портрет, светлая тема, Inter/Roboto, шапка с тональностью/капо, обложка с логотипом бэнда).

### 4.6 Публикация и каталог

-   Видимость: только бэнд / по ссылке / публично.
-   Копирование в другой бэнд создаёт самостоятельную копию.
-   Модерации каталога нет (на старте).

### 4.7 Версионность и удаление

-   Версии с откатом: **песни** и **плейлисты**; статусы: Черновик → Публикация в бэнд → Публикация в каталог.
-   Блокировка при редактировании (optimistic + «кто правит»).
-   Корзина 30 дней.

### 4.8 Оффлайн-кэш

-   Агент немедленно кэширует «следующее событие» и пересобирает при изменениях.
-   Desktop-кэш без версионирования, срок задаёт пользователь (**по умолчанию 7 дней**), хранится в «чистом виде» внутри приложения; экспорт исходных стемов из плеера запрещён.

---

## 5. Архитектура системы

### 5.1 Общая схема

-   **API Gateway** (ASP.NET Core): единая точка входа (HTTPS, JWT+cookie).
-   **Сервисы домена**:
    -   Auth/Identity
    -   Bands & Roles
    -   Songs & Sections & Chord Sheets
    -   Playlists & Events
    -   Files (MinIO) & Presigned URLs
    -   Catalog & Publishing
    -   Import (TXT/DOCX/PDF OCR)
    -   Render Orchestrator (REST) + **Render & Cache Agent** (Worker)
    -   Live Sync (WS для веб; LAN-протокол)
    -   PDF Export
    -   TTS Service (серверный)
    -   Notifications (SMTP)
    -   Updater Appcast (MinIO)
-   **Хранилища**: PostgreSQL (основные данные), MinIO (файлы/артефакты).
-   **Наблюдаемость**: Grafana + Loki; Prometheus метрики.

> Для скорости MVP допускается **модульный монолит** (ASP.NET Core) с вынесенным Агентом.

### 5.2 Микросервисы (целевое разбиение)

1. **Auth Service** — регистрация/логин/refresh, приглашения, API-токены плеера (на бэнд, срок 1 месяц; скоупы: чтение плейлистов, загрузка стемов, Live Master, экспорт).
2. **Bands Service** — бэнды, участники, роли/разрешения, одноразовые ссылки.
3. **Songs Service** — песни, секции, BPM/сигнатуры, редактор аккордов/текста, версии.
4. **Files Service** — MinIO presign (upload/download), контроль сумм, multipart-докачка.
5. **Stems Service** — загрузка/предобработка WAV, группы стемов.
6. **Playlists Service** — плейлисты, события, переходы, структуры поверх песни, версии.
7. **Render Orchestrator** — план «микса» (JSON plan), очередь задач, статусы/артефакты.
8. **Render & Cache Agent** — фоновый воркер (см. раздел 8).
9. **Live Service** — WebSocket для веб-клиентов; LAN-мастер для Desktop.
10. **Import Service** — парсинг TXT/MD/DOCX, PDF (Tesseract OCR ru+en).
11. **PDF Export Service** — генерация листов и книги плейлиста.
12. **TTS Service** — eSpeak NG/Coqui; synth (male/female); WAV-артефакты.
13. **SMTP Service** — отправка писем (восстановление/уведомления).

### 5.3 Технологический стек

-   **Фронтенд**: React + TypeScript, Zustand, shadcn/ui, TanStack Query + Orval/axios.
-   **Бэкенд**: ASP.NET Core (.NET 9), EF Core + PostgreSQL, MinIO S3 SDK, WebSocket, REST.
-   **Аудио на сервере**: FFmpeg (LGPL сборка, без `--enable-gpl`), LAME (LGPL); Rubber Band / SoX **как внешние CLI-процессы** (GPL допустима, т. к. не распространяем серверный бинарь).
-   **Офлайн-плеер (Desktop)**: Avalonia (MIT), PortAudio/PortAudioSharp (MIT/BSD), RtMidi (MIT), SoundTouch (LGPL, **динамически**), Zeroconf.NET (MIT), NetSparkleUpdater (MIT).
-   **OCR**: Tesseract (OSS, ru+en).
-   **Наблюдаемость**: Grafana, Loki (OSS), Prometheus (метрики).

### 5.4 Лицензии и политика совместимости

-   В клиенте: только permissive (MIT/BSD/Apache) + **LGPL (динамически)**; **без GPL/AGPL** линковки.
-   На сервере: GPL-утилиты разрешены **только как отдельные процессы** (Rubber Band/SoX).
-   FFmpeg — **LGPL-сборка**; не подключать GPL-кодеки/фильтры.
-   В дистрибутивах — NOTICE/LICENCE для всех OSS; для внешних CLI — приложить лицензии и ссылки на исходники.

---

## 6. API (сводка, v1)

Префиксы опущены; реальные пути могут отличаться.

-   **Auth**: `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/invitations`, `POST /auth/player-tokens`
-   **Bands**: `GET/POST /bands`, `GET/POST /bands/{id}/members`, `GET/POST /bands/{id}/roles`
-   **Songs**: `GET/POST/PUT/DELETE /songs`, `GET/PUT /songs/{id}/sections`, `POST /songs/{id}/files`, `POST /songs/{id}/publish`
-   **Import**: `POST /import/text`, `POST /import/docx`, `POST /import/pdf` (OCR)
-   **Stems**: `POST /songs/{id}/stems`, `PUT /stems/{id}`, `POST /stems/{id}/group`
-   **Playlists**: `GET/POST/PUT/DELETE /playlists`, `POST /playlists/{id}/items`, `PUT /playlists/{id}/transitions`
-   **Render**: `POST /render/jobs`, `GET /render/jobs?status=...`, `GET /render/jobs/{id}`
-   **Live (WS)**: `GET /live/session` — команды `play/pause/next/seek-section`, таймкод/секция
-   **Files**: `POST /files/presign-upload`, `POST /files/presign-download`

Ответы содержат `contentVersion` для детектирования изменений и инвалидации кэша.

---

## 7. Модель данных (укрупнённо)

-   **Band**(Id, Name, OwnerId)
-   **Role**(Id, BandId, Name, Permissions[])
-   **Member**(UserId, BandId, RoleId, Status)
-   **Song**(Id, BandId, Title, Authors, Key, Bpm, TimeSignatureTrack, ContentVersion, Status)
-   **Section**(Id, SongId, Name, StartBar, BarLength, LocalSignature?)
-   **ChordSheet**(Id, SongId, Body, Notation=CDEFGAB, Capo, Transpose)
-   **Stem**(Id, SongId, GroupId, Kind, FileRef, Version)
-   **StemGroup**(Id, SongId, Name)
-   **Playlist**(Id, BandId, Title, EventDateTime, Venue, ContentVersion)
-   **PlaylistItem**(Id, PlaylistId, SongId, KeyOverride, BpmOverride, StructureOverride, Levels/Pans, Order)
-   **Transition**(Id, PlaylistId, Type=AutoStart|Pause|Crossfade, Params)
-   **RenderJob**(Id, Type, Status, Payload, Artifacts[])
-   **FileObject**(Id, Key, Sha256, Size, Mime)
-   **LiveSession**(Id, BandId, MasterDeviceId, State)
-   **AuditLog**(Id, ActorId, Scope, Action, Ts)

---

## 8. Рендер- и Кэш-агент (сервер)

**Назначение:** немедленная подготовка артефактов «следующего события»; пересборка при изменениях; генерация click/guide; транспонирование/тайм-стретч; микс; загрузка в MinIO.

**Платформа:** .NET 9 Worker Service.  
**Интеграции:** REST к Core-API (Bearer), MinIO (S3), FFmpeg (CLI), Rubber Band/SoX (CLI), TTS HTTP, Prometheus метрики, JSON-логи в Loki.

**Конфиг (суть):**

```yaml
api.baseUrl: https://app.wband.ru/api
api.serviceToken: <Bearer>
agent.parallelJobs: 2
agent.perBandConcurrency: 1
nextEvent.lookAheadHours: 168
minio.endpoint: http://minio:9000
tools.ffmpeg: /usr/bin/ffmpeg
tools.rubberband: /usr/bin/rubberband # опционально
tts.baseUrl: http://tts:8080
export.sampleRate: 44100
export.bitDepth: 24
export.mp3Bitrate: 320
```

**Пайплайн (шаги):**

1. Предзагрузка стемов → WAV 44.1/24 (dynaudnorm)
2. Pitch/tempo: Rubber Band (если есть) или FFmpeg-fallback
3. Click: по BPM/размеру; Guide: TTS (м/ж) по секциям
4. Сборка структуры (concat)
5. Микс (volume/pan/adelay → amix, alimiter)
6. Нормализация/лимитирование → экспорт WAV/MP3 → MinIO → commit в Core-API
7. Инкрементальная пересборка по `contentVersion`

**Состояния:** `queued → claimed → running → (succeeded|failed|canceled)`; heartbeats, retries, идемпотентность по `planHash`.

Метрики Prometheus: `agent_jobs_total{status,type}`, `agent_job_duration_seconds{type}`, `agent_external_tool_seconds{tool}`, `agent_queue_depth`, I/O MinIO счётчики.

---

## 9. Оффлайн-плеер (Desktop)

**Платформы:** Windows/macOS/Linux.  
**Технологии:** Avalonia (MIT), PortAudio/PortAudioSharp (MIT/BSD), RtMidi (MIT), SoundTouch (LGPL, динамически), Zeroconf.NET (MIT), NetSparkleUpdater (MIT).

**Подключение:** по **API-токену группы** (срок 1 месяц; скоупы: чтение плейлистов, загрузка стемов, Live Master, экспорт; без права менять структуру).  
**Функции:** solo/mute, уровни/панорама; маркеры/луп/прыжки; хоткеи/MIDI; текущая/следующая секция; предпросмотр следующей песни; автоскролл текста/аккордов.  
**Live (MVP):** LAN (mDNS+manual), задержки ≤ 20 мс; интернет-Live — позже.  
**Оффлайн-кэш:** хранится в приложении, «в чистом виде»; срок по настройке пользователя (по умолчанию 7 дней). Экспорт исходных стемов запрещён.  
**Обновления:** NetSparkleUpdater + appcast в MinIO; каналы **stable/beta**; подпись **Ed25519** (ключи в CI, публичные ключи в приложении).

---

## 10. Хранение и файлы

-   **MinIO** (S3) для стемов и артефактов. Presigned URL для загрузки/скачивания, контроль SHA256, multipart-докачка. Шифрование «at rest» — **не применяется** на старте.
-   Ключи объектов (пример):

```
bands/{bandId}/songs/{songId}/stems/{stemId}/{version}.wav
bands/{bandId}/playlists/{playlistId}/mixes/{planHash}/mix.wav
bands/{bandId}/playlists/{playlistId}/click/{planHash}/click.wav
bands/{bandId}/playlists/{playlistId}/guide/{planHash}/guide.wav
```

---

## 11. Безопасность

-   Веб-аутентификация: JWT + cookie.
-   API-токены для плеера: **привязка к бэнду**, TTL 1 месяц, отзыв/ротация; скоупы без права менять структуру.
-   Доступ на уровне разрешений бэнда к песням/плейлистам/плейбекам.
-   Аудит: входы, публикации, экспорты, Live-команды.
-   Все протоколы — только по HTTPS.

---

## 12. Наблюдаемость и алерты

-   **Grafana** (обязательно) + **Loki** (логи) + Prometheus (метрики).
-   Дашборды: очередь рендера, длительность задач, ошибки внешних инструментов, I/O MinIO, WebSocket/Live.

---

## 13. CI/CD и окружения

-   **CI:** GitLab CE; **Registry:** Nexus.
-   **Окружения:** stage (`test.wband.ru`), prod (`wband.ru`). Dev — через Aspire.
-   Деплой в Kubernetes — позже (Helm, umbrella-chart, values per env) — вне рамок MVP.

---

## 14. Дорожная карта

-   **MVP:** бэнды/песни/плейлисты; веб-плеер (предпрослушка); Desktop-плеер с LAN-Live; рендер-агент; импорт TXT/DOCX/PDF OCR; PDF-экспорт; версии/корзина; MinIO; Grafana+Loki; SMTP свой; RU/EN UI.
-   **Release 2:** интернет-Live через WebSocket-шлюз; общий каталог и копирование между бэндами; расширенная аналитика; авто-TTS профили.
-   **Release 3:** монетизация/тарифы; мобильные клиенты-зрители; расширенные редакторы и интеграции.

---

## 15. Приложения

### 15.1 Пример план-JSON для рендера песни

```json
{
    "songId": "uuid",
    "planVersion": "v:123",
    "key": "E",
    "transposeSemitones": 2,
    "bpm": 74,
    "timeSignatures": [
        { "bar": 1, "num": 4, "den": 4 },
        { "bar": 57, "num": 6, "den": 8 }
    ],
    "sections": [
        { "name": "Intro", "startBar": 1, "barLength": 4 },
        { "name": "Verse 1", "startBar": 5, "barLength": 16 },
        { "name": "Chorus", "startBar": 21, "barLength": 8 }
    ],
    "structure": [
        "Intro",
        "Verse 1",
        "Chorus",
        "Verse 1",
        "Chorus",
        "Bridge",
        "Chorus"
    ],
    "stems": [
        {
            "id": "s1",
            "group": "Guitars",
            "file": "s3://.../gtr.wav",
            "panL": 1.0,
            "panR": 1.0,
            "gain": -3.0
        },
        {
            "id": "s2",
            "group": "Keys",
            "file": "s3://.../keys.wav",
            "panL": 0.7,
            "panR": 1.0,
            "gain": -1.0
        }
    ],
    "click": { "enabled": true, "accent": 1, "sound": "classic" },
    "guide": { "enabled": true, "voice": "female" },
    "transitions": { "next": "autostart" }
}
```

### 15.2 Пример конечной точки Live (WS)

-   Вход: `{"cmd":"play"|"pause"|"seek-section","section":"Chorus"}`
-   Выход: `{"tick":123456,"bar":17,"section":"Chorus","positionMs":84213}`

### 15.3 Параметры PDF по умолчанию

-   A4, портрет, светлая тема, шрифт Inter/Roboto; шапка: тональность и капо; обложка: логотип бэнда.

---

**Конец спецификации.**
