# DevDesk

[![License: MIT](https://img.shields.io/badge/License-MIT-0B6E4F.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/download)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4.svg)](https://github.com/HadiSiavashi/DevDesk)
[![UI](https://img.shields.io/badge/UI-WinForms-5C2D91.svg)](https://github.com/HadiSiavashi/DevDesk)

Local-first Windows desktop productivity for software engineers. Tasks, projects, Pomodoro, daily planning, notes, snippets, bookmarks, environments, goals, habits, calendar, and analytics live in one WinForms shell — data stays on your machine.

**English and Persian (RTL).** Dark, Light, and System themes.

## Features

- **Tasks & projects** with priorities, tags, checklists, and project workspaces
- **My Day, Daily Plan, Daily Review** for a focused work loop
- **Focus / Pomodoro** with tray notifications
- **Notes, Knowledge Base, snippets, bookmarks, environments**
- **Goals, habits, calendar, and productivity analytics**
- **Global search** (`Ctrl+K`) and **Quick Add** (`Ctrl+Shift+Space`)
- **Local-first** — SQL Server / LocalDB, import/export, SQL backup helpers
- **Persian RTL** and English LTR from Settings

## Screenshots

<p align="center">
  <img src="docs/design/Dashboard%20-%20Expanded%20Sidebar.png" alt="Dashboard" width="720">
</p>

| Tasks | Focus |
| --- | --- |
| <img src="docs/design/Tasks%20-%20Inventory%20%26%20Filtering.png" alt="Tasks"> | <img src="docs/design/Focus%20-%20Focusing%20State.png" alt="Focus"> |

| Notes | Settings |
| --- | --- |
| <img src="docs/design/Notes%20-%20Inventory.png" alt="Notes"> | <img src="docs/design/Settings%20-%20System%20Configuration.png" alt="Settings"> |

More mockups and the design system live in [`docs/design`](docs/design).

## Install

### Windows Setup (recommended)

Download **DevDesk-Setup.exe** from the [latest GitHub Release](https://github.com/HadiSiavashi/DevDesk/releases/latest).

The installer is self-contained (no .NET SDK required). SQL Server Express LocalDB (or SQL Server) is required at runtime.

### Run from source

```bash
git clone https://github.com/HadiSiavashi/DevDesk.git
cd DevDesk
dotnet restore
dotnet build
dotnet run --project DevDesk.WinForms
```

Or open `DevDesk.sln` in Visual Studio and set **DevDesk.WinForms** as the startup project.

First launch migrates the database, seeds demo data when empty, then shows onboarding (name, language, theme, work hours, Pomodoro).

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (source builds only)
- SQL Server LocalDB or SQL Server (Express/Developer/full)
- Visual Studio 2022/2026 or `dotnet` CLI

## Architecture

```
DevDesk.sln
├── DevDesk.WinForms        Presentation (thin forms/controls, theme, localization)
├── DevDesk.Application     Use cases, DTOs, validators, Quick Add parsing
├── DevDesk.Domain          Entities, enums, domain rules, productivity score
├── DevDesk.Infrastructure  Serilog, browser, notifications, backup, Windows helpers
├── DevDesk.Persistence     EF Core DbContext, configurations, migrations, seed
└── DevDesk.Tests           Domain/application/persistence tests (xUnit)
```

Business logic lives in Domain/Application. WinForms coordinates UI and calls application services asynchronously.

> **Naming note:** The task entity is `WorkTask` (table `Tasks`) and status enum is `WorkTaskStatus` to avoid collisions with `System.Threading.Tasks.Task` / `TaskStatus`.

## SQL Server configuration

Connection string is read from `DevDesk.WinForms/appsettings.json` (never hard-coded in source):

```json
{
  "ConnectionStrings": {
    "DevDesk": "Server=(localdb)\\mssqllocaldb;Database=DevDesk;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Database": {
    "AutoMigrate": true,
    "SeedDemoData": true
  }
}
```

Override with environment variables if needed, for example:

```text
ConnectionStrings__DevDesk=Server=localhost;Database=DevDesk;Trusted_Connection=True;TrustServerCertificate=True;
```

Do not commit secrets or password-based connection strings.

## Migrations

Initial migration ships under `DevDesk.Persistence/Migrations`.

With `Database:AutoMigrate` set to `true` (default in appsettings), pending migrations apply on startup.

Manual apply:

```bash
dotnet ef database update --project DevDesk.Persistence --startup-project DevDesk.WinForms
```

Design-time factory: `DevDeskDbContextFactory` (uses LocalDB or `DEVDESK_CONNECTION_STRING`).

Install EF tools if missing:

```bash
dotnet tool install --global dotnet-ef
```

The app never calls `EnsureDeleted()` and never auto-drops user data.

## Building the installer

Produces a self-contained win-x64 publish plus Inno Setup `DevDesk-Setup.exe`:

```powershell
.\scripts\Build-Installer.ps1
```

Or `Build-Setup.bat` from the repo root. Output: `publish\installer\DevDesk-Setup.exe`.

A per-user portable copy (no Setup.exe) is also available:

```powershell
.\scripts\Install-DevDesk.ps1
```

## Running tests

```bash
dotnet test
```

Tests cover task lifecycle, focus recovery, Pomodoro, daily planning, productivity score, Quick Add parsing, and import/export validation/round-trip (EF InMemory).

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl+K` | Global search |
| `Ctrl+Shift+Space` | Quick Add |
| `Ctrl+N` | New item |
| `Ctrl+S` | Save |
| `Esc` | Close overlay/dialog |
| `Space` | Complete selected task |
| `Ctrl+Shift+F` | Start focus |
| `Ctrl+,` | Settings |
| `F1` | Shortcut help |

## Known limitations

- Requires SQL Server/LocalDB; there is no embedded SQLite mode.
- Snippet syntax highlighting is lightweight (plain monospace editor), not a full IDE highlighter.
- Desktop notifications use tray balloons via a 5-minute pending poll plus event-driven Pomodoro/focus alerts (not a cloud push service).
- SQL backup helper needs sufficient SQL Server permissions; failures surface clear instructions rather than pretending success.
- Always on Top / Start minimized / Start with Windows are available in Settings; Start with Windows is **off by default**.
- Calendar Month/Week/Day modes are list-centric around `MonthCalendar` (not a full custom grid calendar control).
- Charting in Analytics uses simple WinForms bar visualizations (no third-party chart suite).
- Import merge skips nested checklists/tags/habit records (top-level entities only).
- NuGet may report `NU1903` advisories for transitive `System.Security.Cryptography.Xml` from EF Core packages.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Security reports: [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE) © 2026 Mohammad Hadi Siavashi
