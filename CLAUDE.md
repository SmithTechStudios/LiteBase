# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

LiteBase is an embeddable SQLite database explorer UI for ASP.NET Core applications — similar to Swagger UI but for SQLite. It consists of two projects:

- **`LiteBase/`** — The NuGet library (target: net8.0). Exposes two ASP.NET middleware components and embeds the built Vue UI as a manifest resource.
- **`DemoApp/`** — A reference ASP.NET Core web app showing how to consume `LiteBase`.

## Commands

### .NET (from repo root or `DemoApp/`)
```bash
dotnet build          # Build solution
dotnet run --project DemoApp   # Run the demo app
dotnet publish        # Publish (triggers NuGet package generation for LiteBase)
```

### Vue UI (from `LiteBase/ui/`)
```bash
pnpm install          # Install dependencies (uses pnpm)
pnpm dev              # Dev server (uses VITE_API_BASE_URL env var for API)
pnpm build            # Build to LiteBase/ui/dist/ — outputs litebase.js + litebase.css
```

After `pnpm build`, the files in `LiteBase/ui/dist/` are embedded as manifest resources in `LiteBase.csproj` and served by the middleware. The `LiteBase/index.html` is also embedded. Rebuilding the .NET project after a UI build is required to pick up updated assets.

## Architecture

### How it works end-to-end

1. Consumer app calls `app.UseLiteBaseUi()` and `app.UseLiteBaseEndpoint("path/to/db.db")` in `Program.cs`.
2. `LiteBaseUIMiddleware` intercepts requests to `/litebase/ui/*` and serves the embedded HTML/JS/CSS directly from the assembly manifest resources.
3. `LiteBaseEndpointMiddleware` intercepts:
   - `GET /litebase/tables` — returns all table names and column metadata via Dapper + raw SQLite query against `sqlite_master`.
   - `GET /litebase/table/{tableName}` — returns all rows from the named table.

### Vue UI (`LiteBase/ui/src/`)

- **`main.ts`** — Bootstraps Vue 3 with PrimeVue (Aura theme), TanStack Vue Query, and mounts `App.vue`.
- **`App.vue`** — Single-page shell: Firebase auth guard, sidebar with table list, main area with column info + paginated DataTable.
- **`components/Login.vue`** — Google sign-in via Firebase Auth popup.
- **`config.ts`** — Reads `VITE_API_BASE_URL` (empty string default means same-origin).
- **`firebase.ts`** — Firebase app + auth initialization (hardcoded config for `boardgame-tracker-2498e` project).

UI stack: Vue 3.5, PrimeVue 4 (Aura preset), TanStack Vue Query 5, Tailwind CSS 4, Axios, VueUse, Iconify (Phosphor icons), Firebase 12, dayjs.

### Key design details

- The UI is completely self-contained — no CDN dependencies at runtime.
- Auth is Firebase Google OAuth; the Firebase JWT is sent as `Authorization: Bearer <token>` on every API request. The backend does **not** currently validate this token — it is UI-only protection.
- Dark mode uses `@vueuse/core`'s `useDark` which toggles the `.dark` class on `<html>`, which PrimeVue's Aura theme responds to via `darkModeSelector: '.dark'`.
- Table name in `/litebase/table/{tableName}` is lowercased before the SQL query (`tableName.ToLower()`).
- The Vite build outputs fixed filenames (`litebase.js`, `litebase.css`) so the C# embedded resource names stay stable.

## Environment Variables (UI)

| Variable | Purpose | Default |
|---|---|---|
| `VITE_API_BASE_URL` | Base URL for API calls | `''` (same origin) |
