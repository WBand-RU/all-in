# Сборка контейнеров MVP

Команды выполняются из корня репозитория. Образы запускают процессы без root.

```bash
docker build -f App/WBand.WebAPI/Dockerfile -t wband-webapi:local .
docker build -f App/WBand.WebProxy/Dockerfile -t wband-proxy:local \
  --secret id=nuget_config,src="$HOME/.nuget/NuGet/NuGet.Config" .
```

BFF использует приватный пакет `IGORock.AuthorizedProxy`. Конфигурация NuGet
должна содержать рабочий feed и учётные данные; BuildKit передаёт её только
на время restore. Не копировать конфигурацию с паролями в build context/образ.
На этой машине split DNS для feed требует дополнительного локального параметра
`--add-host web.nexus.wband.ru:host-gateway`. Это настройка окружения сборки.

API требует PostgreSQL, RabbitMQ и параметры Keycloak из
[WebApiConfiguration](../App/WBand.WebAPI/Configuration/UserServiceConfiguration.cs).
BFF требует `WEB_COMMA_SPLIT_ADDRESSES`, `API_COMMA_SPLIT_ADDRESSES` и
`AUTHORIZED_PROXY_*` из [инструкции BFF](../App/WBand.WebProxy/README.md).
Внутренний порт API/BFF — 8080; `/health` доступен анонимно.
В production браузерный вход обслуживается через HTTPS, secure cookies BFF
требуют TLS. Для сохранения сессий нужны Redis и постоянные ключи Data Protection.

При TLS-подключении к Redis контейнер должен доверять CA сервера. В локальной
проверке Aspire публичные сертификаты скопированы в отдельный каталог, выполнен
`openssl rehash`, каталог подключён read-only через `SSL_CERT_DIR` вместе с
системным хранилищем. Каталог и сертификаты должны читаться пользователем 1654;
абсолютные симлинки из каталога Aspire нужно заменить относительными при копировании.
Проверка TLS не отключалась.

