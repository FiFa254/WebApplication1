# WebApplication1 — Portfolio Showcase

ASP.NET Core MVC portfolio web application for browsing developer profiles and their projects.

## Tech Stack

- ASP.NET Core MVC
- .NET 8
- Entity Framework Core
- SQL Server

## Pages

| Route | Description |
|-------|-------------|
| `/` | Home — hero, stats, latest profiles and projects |
| `/Home/Profiles` | Browse all profiles |
| `/Home/Profile/{id}` | Profile detail with projects |
| `/Home/Projects` | Project gallery |
| `/Home/AddProfile` | Add a new profile with projects |

## Project Structure

- `Controllers/` — MVC controllers
- `Models/` — domain models and view models
- `Views/` — Razor views
- `Data/` — Entity Framework database context
- `Migrations/` — EF Core database migrations
- `wwwroot/` — static CSS, JavaScript, and uploaded images

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server or SQL Server Express LocalDB

### Restore Packages

```bash
dotnet restore
```

### Configure Database

Update the `DefaultConnection` connection string in `appsettings.json` or `appsettings.Development.json` if needed.

### Apply Migrations

```bash
dotnet ef database update
```

If the `dotnet ef` command is not available:

```bash
dotnet tool install --global dotnet-ef
```

### Run the App

```bash
dotnet run
```

Then open the local URL shown in the terminal.

## Build

```bash
dotnet build WebApplication1.sln
```

## Deploy ออนไลน์ฟรี

ใช้ **Render** (แอป Docker) + **Neon** (PostgreSQL) — ดูขั้นตอนใน [DEPLOY.md](./DEPLOY.md)
