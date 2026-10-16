# syntax=docker/dockerfile:1

FROM oven/bun:1.3.14-alpine AS build
WORKDIR /src

COPY App/web/package.json App/web/bun.lock ./
RUN bun install --frozen-lockfile

COPY App/web/ .
RUN bunx --bun vite build

FROM nginx:1.29-alpine AS final
COPY App/web/nginx/default.conf.template /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist /usr/share/nginx/html
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1:8080/health || exit 1
