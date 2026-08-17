# Security Policy

## Supported versions

| Version | Supported |
| --- | --- |
| 1.0.x | Yes |

## Reporting a vulnerability

DevDesk is a local-first desktop app. Please **do not** open a public issue for security problems.

1. Use [GitHub Security Advisories](https://github.com/HadiSiavashi/DevDesk/security/advisories/new) to privately report the issue.
2. Include steps to reproduce, affected version, and impact (data loss, local privilege, injection, etc.).

You should hear back within a few days. Please give time to patch before any public disclosure.

## Scope notes

- Connection strings live in `appsettings.json` or environment variables — never commit secrets.
- The app talks to a local SQL Server / LocalDB instance; treat that database as trusted local data.
- There is no cloud API or user authentication service in this repository.
