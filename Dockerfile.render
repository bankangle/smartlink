# Combined image for Render: one web service running Caddy in front of the .NET
# API and the SvelteKit (adapter-node) server, preserving the same-origin
# topology of the docker-compose stack. Built from the repo root.

# ===== build the .NET API =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY api/SmartLink.Api.csproj .
RUN dotnet restore
COPY api/ .
RUN dotnet publish -c Release -o /out /p:UseAppHost=false

# ===== build the SvelteKit app =====
FROM node:22-bookworm-slim AS web-build
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ .
RUN npm run build && npm prune --omit=dev

# ===== runtime: .NET + Node + Caddy =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Caddy static binary (glibc-friendly Go binary); curl/ca-certs for outbound TLS.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates \
 && curl -fsSL "https://github.com/caddyserver/caddy/releases/download/v2.8.4/caddy_2.8.4_linux_amd64.tar.gz" \
      | tar -xz -C /usr/bin caddy \
 && rm -rf /var/lib/apt/lists/*

# Node runtime binary, copied from the Debian-based build image (glibc-compatible).
COPY --from=web-build /usr/local/bin/node /usr/local/bin/node

COPY --from=api-build /out /app/api
COPY --from=web-build /web/build /app/web/build
COPY --from=web-build /web/node_modules /app/web/node_modules
COPY --from=web-build /web/package.json /app/web/package.json

COPY infra/Caddyfile.render /etc/caddy/Caddyfile
COPY infra/render-start.sh /app/start.sh
RUN chmod +x /app/start.sh

# Internal, same-origin wiring. The public origin is injected at runtime from
# Render's RENDER_EXTERNAL_URL by start.sh.
ENV ASPNETCORE_URLS=http://127.0.0.1:8080 \
    INTERNAL_API_BASE=http://127.0.0.1:8080 \
    PUBLIC_API_BASE="" \
    NODE_ENV=production

CMD ["/app/start.sh"]
