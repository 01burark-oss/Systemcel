# Systemcel

Systemcel is the web version of the pre-accounting product. The repository is private and still contains internal `CashTracker.*` project and namespace names; the shipped product, deployment surface, domains, and documentation use `Systemcel`.

## Project Layout

- `Systemcel.Web`: React/Vite frontend.
- `Systemcel.Api`: ASP.NET Core API and React static file host.
- `CashTracker.Core`: domain entities, models, and service contracts.
- `CashTracker.Infrastructure`: EF Core persistence and integration services.
- `CashTracker.Tests`: service and persistence tests.

Legacy desktop, installer, release, and license-admin code is no longer part of the active product.

## Required Runtime Services

Runtime database is PostgreSQL only. SQLite/local database fallback is intentionally disabled for the API. Tests may still use temporary SQLite databases for speed.

Required production services:

- DigitalOcean App Platform
- DigitalOcean Managed PostgreSQL
- Clerk production application
- Optional Telegram bot integration
- Optional Gemini/OCR and DeepSeek API keys

## Environment Variables

Use `.env.example` as the local/staging/prod checklist. Do not commit real values.

Required:

```text
ASPNETCORE_ENVIRONMENT=Production
SYSTEMCEL_ENVIRONMENT_NAME=production
SYSTEMCEL_DATABASE_CONNECTION_STRING=Host=...;Port=25060;Database=systemcel;Username=...;Password=...;SSL Mode=Require
SYSTEMCEL_ALLOWED_ORIGINS=https://systemcel.app,https://www.systemcel.app
SYSTEMCEL_CLERK_AUTHORITY=https://<clerk-domain>
SYSTEMCEL_CLERK_PUBLISHABLE_KEY=<clerk-publishable-key>
SYSTEMCEL_CLERK_AUTHORIZED_PARTIES=https://systemcel.app,https://www.systemcel.app
```

Admin and integrations:

```text
SYSTEMCEL_ADMIN_CLERK_USER_IDS=
SYSTEMCEL_ADMIN_EMAILS=
Telegram__BotToken=
Telegram__AllowedUserIds=
Telegram__ChatId=
ReceiptOcr__ApiKey=
DeepSeek__ApiKey=
```

For local Vite development, include local origins in `SYSTEMCEL_ALLOWED_ORIGINS` and `SYSTEMCEL_CLERK_AUTHORIZED_PARTIES`, then run the API and Vite dev server separately.

## Local Development

Start PostgreSQL locally or use a development DigitalOcean database, then set `SYSTEMCEL_DATABASE_CONNECTION_STRING`.

API:

```powershell
$env:SYSTEMCEL_DATABASE_CONNECTION_STRING = "Host=localhost;Port=5432;Database=systemcel_dev;Username=systemcel_app;Password=replace-me"
$env:SYSTEMCEL_ALLOWED_ORIGINS = "http://127.0.0.1:5173,http://localhost:5173"
$env:SYSTEMCEL_CLERK_AUTHORITY = "https://<clerk-domain>"
$env:SYSTEMCEL_CLERK_PUBLISHABLE_KEY = "<clerk-publishable-key>"
$env:SYSTEMCEL_CLERK_AUTHORIZED_PARTIES = "http://127.0.0.1:5173,http://localhost:5173"
dotnet run --project .\Systemcel.Api\Systemcel.Api.csproj
```

Frontend:

```powershell
cd Systemcel.Web
npm ci
npm run dev
```

Vite proxies `/api` requests to `http://127.0.0.1:5287`.

## Build and Test

```powershell
dotnet build .\CashTracker.sln --configuration Release
dotnet test .\CashTracker.Tests\CashTracker.Tests.csproj --configuration Release

cd Systemcel.Web
npm ci
npm run build
```

Docker:

```powershell
docker build -t systemcel:local .
docker run --rm -p 8080:8080 --env-file .env systemcel:local
```

Health check:

```text
GET /api/health
```

Runtime frontend config:

```text
GET /api/public/config
```

## DigitalOcean Deployment

Use DigitalOcean App Platform with the root `Dockerfile`.

Production:

- Domain: `systemcel.app`
- Optional alias: `www.systemcel.app`
- Database: production Managed PostgreSQL
- Health check: `/api/health`

Staging:

- Domain: `staging.systemcel.app`
- Database: separate staging Managed PostgreSQL
- Same Docker image, separate environment variables

Deployment checklist:

1. Create or attach the Managed PostgreSQL database.
2. Add every required env var as an encrypted App Platform variable.
3. Set `SYSTEMCEL_ALLOWED_ORIGINS` and `SYSTEMCEL_CLERK_AUTHORIZED_PARTIES` to the exact environment domains.
4. Configure matching allowed origins, redirect URLs, and production keys in Clerk.
5. Deploy the Docker app.
6. Confirm `/api/health` returns 200.
7. Confirm protected `/api/ekran/*` endpoints return 401 without a token.
8. Sign in through Clerk and smoke test dashboard, settings, and accountant flows.

## Repo Hygiene

The repo must not track local or generated artifacts:

- `.env`, `.env.*`, except `.env.example`
- local databases and WAL files
- `node_modules`, `dist`, `bin`, `obj`
- temp folders such as `tmp`, `outputs`, `.runlogs`
- release binaries and archives such as `.exe`, `.zip`, `.sha256`

Run these checks before committing:

```powershell
git status --short
git ls-files | Select-String -Pattern '\.env\.local|\.db$|\.exe$|node_modules|\\dist\\|^tmp/|^outputs/'
git check-ignore -v Systemcel.Web/.env.local tmp outputs Systemcel.Web/dist Systemcel.Web/node_modules sample.db sample.exe
```
