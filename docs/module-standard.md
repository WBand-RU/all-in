# Стандарт серверного модуля WBand

Каждый серверный модуль — отдельный проект `WBand.Modules.<НазваниеМодуля>` и реализует `IWBandModule`. `App/WBand.WebAPI` знает только публичный composition root модуля и его контракты. Прямые ссылки одного модуля на внутренние типы другого запрещены.

## Рекомендуемая структура

```text
WBand.Modules.ExampleModule/
├── Application/       # Wolverine-команды, запросы и обработчики сценариев
├── Contracts/         # Публичные сообщения и HTTP DTO
├── Domain/            # Агрегаты, value objects и доменные правила
├── Endpoints/         # Wolverine HTTP endpoints
├── Infrastructure/    # Marten, внешние клиенты и конфигурация
├── ExampleModule.cs   # Composition root (IWBandModule)
└── WBand.Modules.ExampleModule.csproj
```

Тесты размещаются в `Tests/WBand.Modules.<НазваниеМодуля>.Tests`. Чистую доменную логику покрывайте unit-тестами; HTTP, Marten и Wolverine — интеграционными тестами с изолированной инфраструктурой.

## Границы и данные

- Модуль владеет своей схемой PostgreSQL/Marten и не читает таблицы другого модуля.
- Имя схемы задаётся свойством `IWBandModule.MartenSchemaName`. Все документы модуля регистрируйте в `ConfigureMarten`, например: `options.Schema.For<Song>().DatabaseSchemaName(MartenSchemaName)`. Общая схема из `Marten:SchemaName` предназначена только для инфраструктуры Wolverine.
- Межмодульное взаимодействие выполняется явными Wolverine-сообщениями.
- Публичные контракты стабильны, версионируются и не содержат persistence-моделей.
- Внешние события обрабатываются идемпотентно; побочные сообщения отправляются через durable outbox.
- Все настройки принадлежат модулю, представлены options-классом, валидируются при запуске и имеют production fallback на переменные окружения.

## HTTP и безопасность

HTTP-сценарии оформляются Wolverine endpoint’ами. На границе указывайте авторизацию и валидируйте входные DTO. Keycloak подтверждает личность пользователя; доменные разрешения проверяет соответствующий модуль WBand.
