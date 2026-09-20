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

Current production services:

- Oracle Cloud Always Free VM, with Docker Compose, Caddy and PostgreSQL 18
- Clerk production application
- Optional Telegram bot integration
- Optional OpenAI receipt OCR and DeepSeek API keys

## Environment Variables

Use `.env.example` as the local/staging/prod checklist. Do not commit real values.

Required:

```text
ASPNETCORE_ENVIRONMENT=Production
SYSTEMCEL_ENVIRONMENT_NAME=production
SYSTEMCEL_DATABASE_CONNECTION_STRING=Host=db;Port=5432;Database=systemcel;Username=systemcel_app;Password=...
SYSTEMCEL_ALLOWED_ORIGINS=https://systemcel.app,https://www.systemcel.app
SYSTEMCEL_CLERK_AUTHORITY=https://<clerk-domain>
SYSTEMCEL_CLERK_PUBLISHABLE_KEY=<clerk-publishable-key>
SYSTEMCEL_CLERK_AUTHORIZED_PARTIES=https://systemcel.app,https://www.systemcel.app
```

During a Clerk instance cutover, set `SYSTEMCEL_CLERK_LEGACY_USER_IDS` to the comma-separated development user IDs. A user from that allowlist is relinked to the new production identity only after signing in with the same verified email. Clear the variable after the cutover.

Admin and integrations:

```text
SYSTEMCEL_ADMIN_CLERK_USER_IDS=
SYSTEMCEL_ADMIN_EMAILS=
Telegram__BotToken=
Telegram__AllowedUserIds=
Telegram__ChatId=
ReceiptOcr__ApiKey=
ReceiptOcr__Provider=OpenAI
ReceiptOcr__BaseUrl=https://api.openai.com/v1
ReceiptOcr__Model=gpt-5-mini
DeepSeek__ApiKey=
DeepSeek__ProModel=deepseek-flash
DeepSeek__FlashModel=deepseek-flash
TYPESAFE_API_KEY=
TYPESAFE_BASE_URL=https://api.typesafe.ai/v1
TYPESAFE_MODEL=jev-latest
SYSTEMCEL_SMS_PROVIDER=Netgsm
NETGSM_USERNAME=
NETGSM_PASSWORD=
NETGSM_MSGHEADER=
NETGSM_APPNAME=systemcel
```

DeepSeek ve TypeSafe anahtarları yalnız sunucu ortam değişkenlerinde tutulur. AI sohbeti varsayılan olarak `deepseek-flash` kullanır; Jev kısa sınıflandırma ve eşleştirme kararlarını üstlenerek sohbete gönderilen bağlamı küçültür. Systemcel bilinen işletme verilerini sohbet sağlayıcısına göndermeden önce takma adlarla maskeler.

For local Vite development, include local origins in `SYSTEMCEL_ALLOWED_ORIGINS` and `SYSTEMCEL_CLERK_AUTHORIZED_PARTIES`, then run the API and Vite dev server separately.

## Public Resources and ManyChat

The September 2026 lead magnets are served from stable public pages:

- `/kaynaklar/ai`
- `/kaynaklar/nakit`
- `/kaynaklar/defter`
- `/kaynaklar/takvim`
- `/kaynaklar/50`

Use `Systemcel.Web/public/kaynaklar/manychat-links.csv` when configuring the matching ManyChat keyword replies. The downloadable files live under `Systemcel.Web/public/kaynaklar/dosyalar`; the source builders are in `tools/lead-magnets`.

## Local Development

Start PostgreSQL locally, then set `SYSTEMCEL_DATABASE_CONNECTION_STRING`.

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

## Oracle deployment

The live deployment runs from `deployment/oracle-free/` with Docker Compose. Caddy terminates HTTPS, the app listens on the internal Docker network, and PostgreSQL 18 is not exposed publicly. Use `deployment/oracle-free/scripts/deploy.sh` for a reviewed release and `docs/runbooks/release.md` for the release, backup and recovery procedure. The canonical product scope and pricing rules are in [`docs/product-scope.md`](docs/product-scope.md).

## Developer API

The read-only Developer API v1, API-key handling rules, scopes, pagination, rate limits, and examples are documented in [`docs/developer-api.md`](docs/developer-api.md). The machine-readable contract is [`docs/openapi/developer-api-v1.yaml`](docs/openapi/developer-api-v1.yaml). Write scopes are intentionally unavailable in this MVP.

## Repo Hygiene

The repo must not track local or generated artifacts:

- `.env`, `.env.*`, except `.env.example`
- local databases and WAL files
- `node_modules`, `dist`, `bin`, `obj`
- temp folders such as `tmp`, `outputs`, `.codex-tmp`, `.runlogs`
- release binaries and archives such as `.exe`, `.zip`, `.sha256`

Run these checks before committing:

```powershell
git status --short
git ls-files | Select-String -Pattern '\.env\.local|\.db$|\.exe$|node_modules|\\dist\\|^tmp/|^outputs/'
git check-ignore -v Systemcel.Web/.env.local tmp outputs .codex-tmp Systemcel.Web/dist Systemcel.Web/node_modules sample.db sample.exe
```
