# Сообщения MVP: аудит перед изменениями

Статус: аудит выполнен; публикации профилей удалены (9 целевых тестов прошли),
остальные изменения доставки ещё не внесены. Рассматриваются
только проекты текущего `WBand.slnx` и обработчики, подключённые AppHost/API.
Исторические playback/stem-проекты не являются потребителями MVP.

| Сообщения | Текущая публикация | Потребители MVP | Решение |
| --- | --- | --- | --- |
| UserCreated, UserLoggedIn | UserProfileSynchronization, KeycloakEventHandler; после commit | Нет | Публикации удалены; внешние маршруты будут удалены в T06a.5 |
| BandCreated, MemberInvited, MembershipChanged | BandEndpoints, InvitationEndpoints, MemberEndpoints; после commit | Нет | Удалить публикацию и внешние маршруты |
| SongCreated, SongChanged, SongDeleted, SongRestored | SongEndpoints, SongHistoryEndpoints; после commit | Нет | Удалить публикацию и внешние маршруты |
| SongPartitionsChanged, SongSectionsChanged, SongContentChanged | PartitionEndpoints, SongSectionEndpoints, SongContentEndpoints; после commit | Нет; PlaybackInvalidationHandlers находятся вне активного решения | Удалить публикацию и внешние маршруты |
| KeycloakEvent REGISTER/LOGIN | Внешний провайдер Keycloak → RabbitMQ | KeycloakEventHandler | Сохранить входящий маршрут и повторы |
| CheckBandPermission, GetUserBandIds, GetSongReferences и адаптеры EventModule | Wolverine InvokeAsync, синхронные запросы | Band/Song integration handlers, EventIntegrationHandler | Сохранить синхронные контракты; outbox не нужен |

Исходящие события сейчас подтверждаются брокером после commit EF Core.
Недоступность брокера способна вернуть HTTP 500 для уже сохранённой операции.
Такое сообщение об откате недопустимо. Обязательных исходящих асинхронных
сценариев в текущем MVP не обнаружено: добавлять outbox для этих публикаций
не требуется. Контракты можно сохранить до переноса модулей, но не отправлять
сообщения без потребителя.

Входящие REGISTER/LOGIN используют транзакционную advisory-блокировку по subject,
уникальность keycloak_id и проверку LastLoginAt: повтор REGISTER не создаёт строку,
повтор/старый LOGIN не уменьшает дату входа. Отдельный inbox пока не нужен.
EnsureProfile восстанавливает пропущенную регистрацию из проверенных claims.
Дубли и нарушенный порядок проверяются существующими интеграционными тестами;
их проверки данных нужно сохранить при удалении исходящих публикаций.

Перед закрытием T06a нужны отрицательный сценарий недоступной публикации после
commit (успешные band/song HTTP и сохранённые данные), повторная доставка
REGISTER/LOGIN, отсутствие внешних маршрутов для перечисленных событий и
полный Release-набор. Изменения разбиты в tasks/todo.md.

T06a.2a: публикация BandCreated удалена. HTTP-fixture использует MetadataRules
Wolverine 6.49.0 (`IEnvelopeRule`), чтобы воспроизводить отказ исходящей отправки.
До правки запрос создания группы возвращал 500 после commit; после правки
успешно сохраняются группа и Owner. Все 20 BandsApiTests прошли.
