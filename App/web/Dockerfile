# syntax=docker/dockerfile:1

FROM oven/bun:1.3.14-alpine AS build
WORKDIR /src

COPY App/web/package.json App/web/bun.lock ./
RUN bun install --frozen-lockfile

COPY App/web/ .
RUN bunx --bun vite build

FROM nginx:1.29-alpine AS final
ENV WEBAPI_URL=http://wband-webapi:8080 \
    VITE_KEYCLOAK_URL=https://auth.wband.ru \
    VITE_KEYCLOAK_REALM=wband-dev \
    VITE_KEYCLOAK_CLIENT_ID=web
COPY App/web/nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY App/web/nginx/runtime-config.js.template /etc/wband/runtime-config.js.template
COPY App/web/nginx/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
COPY --from=build /src/dist /usr/share/nginx/html
RUN sed -i 's/\r$//' /docker-entrypoint.d/40-runtime-config.sh \
    && chmod +x /docker-entrypoint.d/40-runtime-config.sh
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1:8080/health || exit 1
