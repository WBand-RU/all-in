# Аудит сообщений MVP

T06a завершена. Рассмотрены проекты `WBand.slnx` и обработчики текущего
AppHost/API. Исторические playback/stem-проекты не являются потребителями MVP.

| Сообщения | Прежнее место публикации | Потребители MVP | Текущее решение |
| --- | --- | --- | --- |
| UserCreated, UserLoggedIn | UserProfileSynchronization, KeycloakEventHandler | Нет | Публикации и внешние маршруты удалены |
| BandCreated, MemberInvited, MembershipChanged | BandEndpoints, InvitationEndpoints, MemberEndpoints | Нет | Публикации и внешние маршруты удалены |
| SongCreated, SongChanged, SongDeleted, SongRestored | SongEndpoints, SongHistoryEndpoints | Нет | Публикации и внешние маршруты удалены |
| SongPartitionsChanged, SongSectionsChanged, SongContentChanged | PartitionEndpoints, SongSectionEndpoints, SongContentEndpoints | Нет; PlaybackInvalidationHandlers вне активного решения | Публикации и внешние маршруты удалены |
| KeycloakEvent REGISTER/LOGIN | Внешний провайдер Keycloak → RabbitMQ | KeycloakEventHandler | Сохранены именованный входящий broker, повторы и идемпотентная запись |
| CheckBandPermission, GetUserBandIds, GetSongReferences и адаптеры EventModule | Синхронные Wolverine InvokeAsync | Band/Song integration handlers, EventIntegrationHandler | Сохранены синхронные контракты |

Раньше подтверждение исходящей отправки ожидалось после commit EF Core.
Отказ брокера возвращал ошибку для уже сохранённой операции. Обязательных
исходящих асинхронных сценариев MVP нет, поэтому ненужные отправки удалены,
а outbox не добавлен. Conventional routing для исходящих сообщений отключён.
Контракты пока сохранены для дальнейшего переноса модулей.

REGISTER/LOGIN используют транзакционную advisory-блокировку по subject,
уникальность keycloak_id и проверку LastLoginAt. Повтор REGISTER не создаёт
строку, повтор/старый LOGIN не уменьшает дату входа. Отдельный inbox не нужен.
EnsureProfile восстанавливает пропущенную регистрацию из проверенных claims.
`Services/KeycloakMessagesReader.cs` внутри проекта WebAPI — прежний
незарегистрированный BackgroundService; активной доставки через него нет.

Проверки:

- Полный Release-набор: **103/103** (79 API/database, 24 auth/BFF).
- Отказ исходящей отправки воспроизведён до исправления: HTTP 500 после commit.
  После исправления проверены профили, группы, приглашения, членство, метаданные,
  история, секции, партии, отдельные ячейки и атомарное сохранение партии.
- HTTP-fixture задаёт явный тестовый адресат и IEnvelopeRule. Отдельная проба
  подтверждает реальный отказ отправки: удаление подписчиков не ослабляет проверку.
- При остановленном RabbitMQ через BFF созданы группа, песня, секция, партия и
  текст; все записи и чтение вернули 200, текст совпал. Затем RabbitMQ запущен,
  API и broker Healthy; в логе подтверждено восстановление соединения.

Журналы этой сессии: `/tmp/wband-t06a5-final-tests.log`,
`/tmp/wband-t06a5-outage-current.json`, `/tmp/wband-t06a5-api-recovery.log`.


После T11 raw `KeycloakEvent` находится в `BandModule.Contracts`, а обработчик
и профильная синхронизация — в `BandModule.Application`. Именованный broker
и JSON wire format сохранены. Реальный LOGIN после перезапуска обновил профиль;
идемпотентность и запрет ненужных публикаций по-прежнему проверяются тестами.
