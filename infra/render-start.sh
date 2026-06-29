#!/usr/bin/env bash
# Entrypoint for the combined Render web service: start the .NET API and the
# SvelteKit server on internal ports, then Caddy out front on $PORT.
set -uo pipefail

# Render gives us the public URL at runtime; everything OAuth/CSRF-related is
# derived from it so we never hard-code the hostname.
ORIGIN_URL="${RENDER_EXTERNAL_URL:-http://localhost:${PORT:-10000}}"
export ORIGIN="$ORIGIN_URL"
export AppUrls__WebBaseUrl="$ORIGIN_URL"
export Spotify__RedirectUri="$ORIGIN_URL/api/spotify/callback"
export Deezer__RedirectUri="$ORIGIN_URL/api/deezer/callback"

echo "[start] public origin: $ORIGIN_URL"

# .NET API — listens on 127.0.0.1:8080 (ASPNETCORE_URLS set in the image).
dotnet /app/api/SmartLink.Api.dll &

# SvelteKit (adapter-node) — listens on 127.0.0.1:3000.
PORT=3000 HOST=127.0.0.1 node /app/web/build &

# Caddy — the only Render-facing listener, on $PORT.
caddy run --config /etc/caddy/Caddyfile --adapter caddyfile &

# If any of the three exits, stop the container so Render restarts it cleanly.
wait -n
echo "[start] a process exited — shutting the container down for a restart."
exit 1
