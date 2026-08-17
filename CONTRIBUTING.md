# Contributing to DevDesk

Thanks for taking the time to contribute. DevDesk is a local-first Windows WinForms app; keep changes focused and consistent with the existing layers.

## Prerequisites

- Windows 10/11
- .NET 10 SDK
- SQL Server LocalDB (or SQL Server) for running the app
- `dotnet` CLI or Visual Studio 2022/2026

## Build and test

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project DevDesk.WinForms
```

Do not commit `bin/`, `obj/`, `publish/`, `tools/`, `.vs/`, or log files.

## Architecture

| Project | Responsibility |
| --- | --- |
| `DevDesk.Domain` | Entities, enums, domain rules |
| `DevDesk.Application` | Use cases, DTOs, validators, Quick Add |
| `DevDesk.Persistence` | EF Core, migrations, seed |
| `DevDesk.Infrastructure` | Logging, tray, backup, Windows helpers |
| `DevDesk.WinForms` | UI only — call application services asynchronously |

Business logic belongs in Domain/Application, not in forms.

The task entity is `WorkTask` / `WorkTaskStatus` (table `Tasks`) to avoid collisions with `System.Threading.Tasks`.

## UI and localization

- Themes: Dark, Light, System — use `ThemeManager` / existing themed controls.
- Languages: `en-US` (LTR) and `fa-IR` (RTL). New user-visible strings go in `DevDesk.WinForms/Localization/UiCatalog.cs` for **both** cultures.
- Layout must remain usable in RTL (sidebar docks, text alignment, shortcuts overlay).

## Pull requests

1. Open an issue first for larger features.
2. Keep PRs small and describe *why*.
3. Include tests when you change domain/application behavior.
4. Do not add password-based connection strings or secrets.
5. Run `dotnet test` before opening the PR.

Installer changes: `installer/DevDesk.iss` and `scripts/Build-Installer.ps1`. Build with `.\scripts\Build-Installer.ps1`.
