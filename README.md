# DevFolio — Developer Portfolio Showcase

ASP.NET Core MVC web app for browsing developer profiles and the projects they have built.
Anyone can browse; a single admin account adds, edits, and deletes profiles.

## Tech Stack

- ASP.NET Core MVC, .NET 8
- Entity Framework Core 8 — **PostgreSQL** (production / Docker) or **SQL Server** (local development)
- Cookie authentication for the admin, rate-limited sign-in
- Docker + docker-compose, xUnit integration tests

## Pages

| Route | Access | Description |
|-------|--------|-------------|
| `/` | public | Home — hero, stats, latest profiles and projects |
| `/Home/Profiles?page=n` | public | All profiles, 12 per page |
| `/Home/Profile/{id}` | public | Profile detail with projects |
| `/Home/Projects?page=n` | public | Project gallery, 12 per page |
| `/Account/Login` | public | Admin sign-in (5 attempts per minute per IP) |
| `/Home/AddProfile` | admin | Add a profile with up to 20 projects and a photo |
| `/Home/EditProfile/{id}` | admin | Edit a profile, replace or remove its photo |
| `/Home/DeleteProfile/{id}` | admin (POST) | Delete a profile, its projects, and its photo |
| `/health` | public | Health check (database connectivity) |

## Project Structure

- `Controllers/` — `HomeController` (pages + admin actions), `AccountController` (sign-in / sign-out)
- `Models/` — entities, view models, `PagedList`
- `Views/` — Razor views
- `Data/` — `ApplicationDbContext`
- `Infrastructure/` — database provider selection, admin credentials, image storage, rate limiting, security headers
- `Migrations/` — EF Core migrations for SQL Server
- `DevFolio.PostgresMigrations/` — EF Core migrations for PostgreSQL
- `DevFolio.Tests/` — xUnit integration tests (in-memory database)
- `wwwroot/` — static CSS / JavaScript

## Run with Docker (recommended)

```bash
cp .env.example .env
docker compose build app
docker run --rm devfolio:latest hash-password 'YourStrongPassword'   # paste the output into ADMIN_PASSWORD_HASH in .env
docker compose up -d
```

Open <http://localhost:8080>. Sign in at `/Account/Login` with `ADMIN_USERNAME` and the password you hashed.

- Data: PostgreSQL volume `db-data`; uploaded photos and data protection keys in volume `app-data` (`/data` in the container).
- Migrations run automatically on start.
- The container serves plain HTTP. For HTTPS put a reverse proxy (Caddy, nginx, Traefik) in front and set `FORWARDED_HEADERS=true`.

## Run locally (SQL Server LocalDB)

Prerequisites: .NET 8 SDK (or newer), SQL Server LocalDB.

```bash
dotnet run -- hash-password 'YourStrongPassword'        # copy the printed hash
dotnet user-secrets set "Admin:Username" "admin"
dotnet user-secrets set "Admin:PasswordHash" "<hash>"
dotnet run
```

The Development database (`DevFolioDb_Dev`) is created and migrated on start.

## Configuration

| Setting (env var) | Default | Purpose |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | LocalDB | SQL Server or PostgreSQL (`Host=...`) connection string |
| `DATABASE_URL` | — | `postgres://` URL (Render / Neon); overrides the connection string |
| `Admin__Username`, `Admin__PasswordHash` | — | Admin account. If either is missing, admin sign-in is disabled |
| `Storage__UploadsPath` | `wwwroot/uploads` | Folder for uploaded photos (served at `/uploads`) |
| `DataProtection__KeysPath` | — | Folder for auth / antiforgery keys so sign-ins survive restarts |
| `ForwardedHeaders__TrustAll` | `false` | Trust `X-Forwarded-*` — only behind a reverse proxy |
| `Hosting__UseHttpsRedirection` | `true` | Redirect HTTP to HTTPS (skipped when `PORT` is set) |
| `RateLimiting__LoginPerMinute` | `5` | Sign-in attempts per minute per IP |
| `PORT` | — | Listen on `http://+:PORT` (set by Render) |
| `Seed__DemoData` | `false` | Add 3 fictional sample profiles when the database is empty (public demo) |

## Tests

```bash
dotnet test DevFolio.sln
```

## Adding a migration

Model changes need a migration for **both** providers:

```bash
dotnet ef migrations add <Name>
DEVFOLIO_PG="Host=localhost;Database=DevFolioDb;Username=postgres;Password=..." \
  dotnet ef migrations add <Name> --project DevFolio.PostgresMigrations --startup-project DevFolio.PostgresMigrations
```

## Deploy on Render + Neon

See [DEPLOY.md](./DEPLOY.md).
