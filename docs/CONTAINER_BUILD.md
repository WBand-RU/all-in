# Сборка контейнеров MVP

Команды выполняются из корня репозитория. Образы запускают процессы без root.

```bash
docker build -f App/WBand.WebAPI/Dockerfile -t wband-webapi:local .
docker build -f App/WBand.WebProxy/Dockerfile -t wband-proxy:local \
  --secret id=nuget_config,src="$HOME/.nuget/NuGet/NuGet.Config" .
docker build -f App/web/Dockerfile -t wband-web:local \
  --build-arg BFF_ORIGIN=http://proxy:8080 .
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

Веб использует Bun 1.4.0 для lockfile v2, Node.js 24 и Next.js standalone.
В финальный образ включены `server.js`, `public` и `.next/static`;
зависимости сборки не переносятся целиком.

Внешняя точка входа — BFF: он направляет страницы в web, API-запросы — в API.
`BFF_ORIGIN` в web задаёт внутренний адрес BFF для проверки сессии на сервере.
Значение по умолчанию — `http://proxy:8080`; при других именах сервисов
передать нужное значение и при сборке (`--build-arg`), и при запуске (`-e`).
Next.js фиксирует rewrites при сборке, поэтому runtime-переменная сама по себе
не меняет адрес этих маршрутов. Для локальной проверки с BFF на хосте
использовался `http://localhost:4000` и сеть host.

Источник поведения standalone: [Next.js output](https://nextjs.org/docs/app/api-reference/config/next-config-js/output)
и документация установленного пакета `next/dist/docs`.

## CI

Активные jobs проверяют API, BFF, Next.js и все .NET-тесты решения.
Изменения legacy worker, `Services`, `src`, `App/web-vite` не запускают MVP-сборки.
Глобальные конфигурации и изменения workflow запускают все проверки.

Для backend-тестов и сборки BFF репозиторию нужен GitHub Actions secret
`NUGET_CONFIG`: полный XML NuGet.Config с доступом к приватному feed.
Скрипт `.github/scripts/configure-nuget.sh` создаёт файл вне checkout с правами
0600; Docker получает его через `secret-files`. Содержимое не выводится в журнал.
Без секрета job завершается с понятной ошибкой. Секреты fork PR автоматически
недоступны; такие PR требуют проверки в доверенном окружении с доступом к feed.

Публикация использует прежние ветки staging/release и registry secrets.
Для BFF добавлена переменная `SWARM_PROXY_SERVICE`; остальные имена сервисов
задаются `SWARM_WEBAPI_SERVICE` и `SWARM_WEB_SERVICE`. Публикация и deployment
в текущей локальной проверке не запускались; repository secrets не изменялись.
