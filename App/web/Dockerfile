# syntax=docker/dockerfile:1
FROM oven/bun:1.4.0 AS bun
FROM node:24-bookworm-slim AS build
COPY --from=bun /usr/local/bin/bun /usr/local/bin/bun
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1
COPY App/web/package.json App/web/bun.lock ./
RUN bun install --frozen-lockfile
COPY App/web/ ./
ARG BFF_ORIGIN=http://proxy:8080
ENV BFF_ORIGIN=$BFF_ORIGIN
RUN bun run build

FROM node:24-bookworm-slim AS final
WORKDIR /app
ENV NODE_ENV=production NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 HOSTNAME=0.0.0.0 BFF_ORIGIN=http://proxy:8080
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static
COPY --from=build --chown=node:node /app/public ./public
USER node
EXPOSE 3000
CMD ["node", "server.js"]
