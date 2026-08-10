# DevDesk

DevDesk is a local-first Windows desktop productivity application for software engineers. It combines task management, project tracking, focus/Pomodoro sessions, daily planning and review, notes, snippets, bookmarks, environments, goals, habits, calendar, and productivity analytics in one WinForms shell.

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

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB or SQL Server (Express/Developer/full)
- Visual Studio 2022/2026 or `dotnet` CLI

## Setup

```bash
git clone <repo-url> DevDesk
cd DevDesk
dotnet restore
dotnet build
```

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

## Running the application

```bash
dotnet run --project DevDesk.WinForms
```

Or open `DevDesk.sln` in Visual Studio and set **DevDesk.WinForms** as the startup project.

First launch:

1. Initializes/migrates the database
2. Seeds demo data when the database is empty
3. Shows onboarding (display name, language, theme, work hours, Pomodoro)
4. Opens the Dashboard

Logs are written to `logs/devdesk-.log` under the app working directory.

## Running tests

```bash
dotnet test
```

Tests cover task lifecycle, focus recovery, Pomodoro, daily planning, productivity score, Quick Add parsing, and import/export validation/round-trip (EF InMemory).

## Project structure (high level)

| Area | Location |
|------|----------|
| Entities / enums | `DevDesk.Domain` |
| Services / DTOs / Quick Add | `DevDesk.Application` |
| EF Core + seed | `DevDesk.Persistence` |
| Logging / backup / tray notifications | `DevDesk.Infrastructure` |
| Main shell + views | `DevDesk.WinForms` |
| Tests | `DevDesk.Tests` |

### Main UI modules

Dashboard, My Day, Tasks, Projects, Calendar, Focus/Pomodoro, Notes, Goals, Habits, Snippets, Bookmarks, Environments, Knowledge Base, Analytics, Settings, Daily Planning/Review, Global Search (`Ctrl+K`), Quick Add (`Ctrl+Shift+Space`).

### Themes & localization

- Themes: Dark, Light, System (slate/cyan developer palette)
- Languages: English (LTR), Persian (RTL)

## Keyboard shortcuts

| Shortcut | Action |
|----------|--------|
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
- Task entity is named `WorkTask` / status `WorkTaskStatus` (table still `Tasks`) to avoid `System.Threading.Tasks` collisions.

## License

Use and modify for your own productivity workflows.
