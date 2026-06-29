# SmartLink — smart links & pre-saves for music artists

One link for a release that lists every streaming platform, tracks which one each
visitor picks, and offers a real Deezer pre-save — with a small admin to create
pages and read the click data. Think Hypeddit / feature.fm, at MVP scope but
shipped end-to-end against a real database.

---

## What it does

- **Smart link pages** — a public, SEO-friendly landing page per track/release at `/<slug>`,
  listing Spotify, Apple Music, YouTube Music, Amazon Music, Deezer, Tidal and more.
- **One-paste setup** — paste a single Spotify/Apple URL in the admin; the
  [Odesli/song.link](https://odesli.co) API resolves every other platform automatically,
  along with title, artist and cover art.
- **Spotify & Deezer pre-save** — visitors authorise with the DSP (OAuth 2.0) and the track
  is added straight to their library via the platform API. A real, demoable save — no label
  deal needed. Track IDs are picked up automatically from the Odesli resolve. Spotify is the
  default live target; Deezer is wired and ready behind a one-line flag.
- **Per-platform click tracking** — every outbound click goes through a `302` redirect
  endpoint that logs the platform, timestamp and device type before forwarding.
- **Admin** — login, create/edit/delete pages, live preview while editing, and an analytics
  view (total clicks, pre-saves, clicks-by-platform, last-14-days chart).
- **Mobile-first, on-brand design** — hand-built components, no UI kit. Each page themes
  itself from its artwork (see below).

### The surprise feature: artwork-driven theming

When a page is created, the API downloads the cover art, picks the most vibrant
representative color (saturation- and brightness-weighted), and stores it. The public
page builds its whole gradient/glow from that single accent, so every link page feels
designed for *that* release without any manual work. See
[`api/Services/ColorExtractor.cs`](api/Services/ColorExtractor.cs) and
[`web/src/lib/theme.ts`](web/src/lib/theme.ts).

---

## Stack & why

| Layer    | Choice | Why |
|----------|--------|-----|
| Frontend | **SvelteKit + TypeScript + Tailwind** | SSR so link pages are crawlable/shareable (OG tags) and fast on mobile; hand-written components for a non-template look. |
| Backend  | **ASP.NET Core (.NET 10) Minimal APIs + EF Core** | Typed, fast, clean DI; Minimal APIs keep the surface small for an MVP. |
| Database | **PostgreSQL** | Solid relational fit for pages → links/clicks; nothing here needs more. Redis was intentionally skipped — no caching/queue need at this scale. |
| Links    | **Odesli / song.link** | Free, no key; turns one URL into every DSP link at create-time so we never scrape. |
| Pre-save | **Spotify + Deezer OAuth** | Both implemented behind one interface (consent → exchange → add-to-library), with a clickable demo fallback when no credentials are set. Spotify dev apps allow a 25-user allowlist (set tester emails in the dashboard); Deezer needs no allowlist but has paused new app registration. Apple Music needs a $99/yr membership, so it's left as a follow-up. |
| Deploy   | **Docker Compose + Caddy** | One command to stand up the whole stack; Caddy gives automatic HTTPS on a VDS. |

---

## Architecture

```
                         ┌─────────────┐
  browser ──HTTPS──▶ Caddy reverse proxy
                         │   /api/* ──────────▶ ASP.NET Core API ──▶ PostgreSQL
                         │   /*     ──────────▶ SvelteKit (SSR, adapter-node)
                         └─────────────┘                │
                                                        ├─▶ Odesli (resolve links + artwork)
                                                        └─▶ Spotify / Deezer (OAuth + add-to-library)
```

- The SvelteKit server renders pages and talks to the API **server-to-server**
  (`INTERNAL_API_BASE`). The browser only ever calls the same origin; Caddy routes
  `/api/*` to the backend. The JWT admin token lives in an **httpOnly cookie** — it's
  never exposed to client JS.
- Click redirects (`/api/r/...`) and the Spotify/Deezer OAuth bounce are plain navigations
  to the API, so tracking works even with JS disabled.

### Data model

`Page` ──< `PlatformLink`, `Page` ──< `ClickEvent`, `Page` ──< `SpotifySave`/`DeezerSave`, plus `AdminUser`.
See [`api/Models`](api/Models).

### API surface

```
Public
  GET  /api/pages/{slug}                 resolved page (published only)
  GET  /api/r/{pageId}/{platform}        log click → 302 to the DSP
  GET  /api/spotify/login?pageId&trackId start Spotify OAuth
  GET  /api/spotify/callback             finish OAuth, add to library, bounce back
  GET  /api/deezer/login?pageId&trackId  start Deezer OAuth
  GET  /api/deezer/callback              finish OAuth, add to library, bounce back

Admin (JWT bearer)
  POST   /api/admin/login                → { token }
  POST   /api/admin/resolve              Odesli preview for the editor
  GET    /api/admin/pages                list + click totals
  POST   /api/admin/pages                create
  GET    /api/admin/pages/{id}           full page for editing
  PUT    /api/admin/pages/{id}           update
  DELETE /api/admin/pages/{id}           delete
  GET    /api/admin/pages/{id}/stats     aggregated analytics
```

---

## Run it

### Option A — full stack in Docker (closest to production)

```bash
cd infra
cp .env.example .env        # then edit secrets (JWT_KEY, ADMIN_PASSWORD, …)
docker compose up -d --build
```

With the defaults in `.env.example` (`SITE_ADDRESS=:80`, plain HTTP) the app is at
**http://localhost** — a demo page is auto-seeded at **/demo**, and the admin is at
**/admin** (default `admin` / the `ADMIN_PASSWORD` you set).

For a real server, set `SITE_ADDRESS=your.domain` and `PUBLIC_ORIGIN=https://your.domain`
in `.env`; Caddy provisions HTTPS automatically.

### Option B — local dev (hot reload)

```bash
# 1. Postgres only, in Docker (host port 5433 to avoid clashing with a local Postgres)
docker compose -f infra/docker-compose.dev.yml up -d

# 2. API  → http://localhost:5080
cd api && dotnet run

# 3. Web  → http://localhost:5180
cd web && npm install && npm run dev
```

Migrations apply and the admin user is seeded automatically on API start.

---

## Configuration

All backend config is env-overridable (`Section__Key`). Key values:

| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__Postgres` | DB connection |
| `Jwt__Key` | admin token signing secret (**set in production**) |
| `AdminSeed__Username` / `__Password` | seeded admin login |
| `AppUrls__WebBaseUrl` | where the OAuth callbacks bounce visitors back to |
| `Spotify__ClientId` / `__ClientSecret` / `__RedirectUri` | Spotify pre-save credentials |
| `Deezer__AppId` / `__SecretKey` / `__RedirectUri` | Deezer pre-save credentials |
| `DemoSeed__Enabled` | seed the `/demo` page on first boot |

Web: `INTERNAL_API_BASE` (SSR target), `PUBLIC_API_BASE` (browser links; empty = same
origin), `ORIGIN` (adapter-node CSRF origin).

### Enabling Spotify pre-save (default live target)

1. Create a free app at <https://developer.spotify.com/dashboard>.
2. Set its redirect URI to `<PUBLIC_ORIGIN>/api/spotify/callback`.
3. Put the Client ID + Secret into `.env` (`SPOTIFY_CLIENT_ID`, `SPOTIFY_CLIENT_SECRET`).
4. The app starts in *development mode* — add each tester's Spotify account email under
   **User Management** (up to 25). Those users can then pre-save to their real library.

Track IDs are filled in automatically when you resolve a Spotify/Apple/etc. link in the
editor; you can also paste a Spotify track ID by hand on the Spotify row.

### Enabling Deezer pre-save

1. Create a free app at <https://developers.deezer.com/myapps>.
2. Set its redirect URL to `<PUBLIC_ORIGIN>/api/deezer/callback`.
3. Put the App ID + Secret into `.env` (`DEEZER_APP_ID`, `DEEZER_SECRET_KEY`).
4. Flip `DEEZER_PRESAVE_ENABLED` to `true` in [`web/src/routes/[slug]/+page.svelte`](web/src/routes/%5Bslug%5D/+page.svelte) to show the button.

Without credentials either pre-save button still renders and runs in **demo mode** — it
records the intent and shows a "(demo mode)" toast, so the flow stays fully clickable.

---

## What's next

- **More pre-save targets** — Apple Music (needs the paid Apple Developer membership) is
  scoped but not wired up. Spotify and Deezer are both implemented.
- **Scheduled pre-saves** — today's flow adds to the library immediately; true
  save-on-release-day for unreleased tracks needs stored refresh tokens + a background job.
- **Richer analytics** — referrers, geo, unique vs. total, CSV export.
- **Multi-tenant** — per-artist accounts and roles (currently a single seeded admin).
- **Tests** — endpoint integration tests and a couple of Playwright happy-paths.
- **Custom domains** per artist, QR codes for posters.

---

## Project layout

```
api/    ASP.NET Core API — Models, Data (EF + seeder), Services, Endpoints
web/    SvelteKit app — routes (public + /admin), lib (components, theme, platforms)
infra/  Dockerfiles compose: dev (Postgres) + prod (db/api/web/caddy), Caddyfile, .env.example
```
