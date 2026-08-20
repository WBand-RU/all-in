# Инфраструктура WBand

## CORS для прямой загрузки в MinIO

Фронтенд загружает WAV-файлы напрямую по presigned URL, поэтому bucket должен разрешать `PUT` с доменов WBand. Пример политики для `mc cors set <alias>/<bucket> minio-cors.xml`:

```xml
<CORSConfiguration>
  <CORSRule>
    <AllowedOrigin>http://localhost:3000</AllowedOrigin>
    <AllowedOrigin>https://test.wband.ru</AllowedOrigin>
    <AllowedOrigin>https://wband.ru</AllowedOrigin>
    <AllowedMethod>PUT</AllowedMethod>
    <AllowedMethod>GET</AllowedMethod>
    <AllowedHeader>content-type</AllowedHeader>
    <ExposeHeader>etag</ExposeHeader>
    <MaxAgeSeconds>3600</MaxAgeSeconds>
  </CORSRule>
</CORSConfiguration>
```

Не используйте `AllowedOrigin=*` для production bucket. Download URL открывается напрямую, а доступ к нему перед подписанием проверяет `FileModule`.
