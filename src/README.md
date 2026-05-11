# Strawhats Framework Templates

[![ci](https://github.com/StrawhatsCompany/sh-framework-template/actions/workflows/ci.yml/badge.svg)](https://github.com/StrawhatsCompany/sh-framework-template/actions/workflows/ci.yml)
[![release](https://github.com/StrawhatsCompany/sh-framework-template/actions/workflows/release.yml/badge.svg)](https://github.com/StrawhatsCompany/sh-framework-template/actions/workflows/release.yml)
[![NuGet](https://img.shields.io/nuget/v/StrawhatsCompany.SHFramework.Templates.svg)](https://www.nuget.org/packages/StrawhatsCompany.SHFramework.Templates)

A `dotnet new` template (`shf`) that scaffolds a Clean Architecture .NET 10 service: CQRS, minimal API, vertical-slice features, provider pattern, OpenAPI metadata, health checks, exception handling, rate limiting, and a secrets convention built around user-secrets.

## Install

```powershell
dotnet new install StrawhatsCompany.SHFramework.Templates
```

## Scaffold a service

```powershell
dotnet new shf -n Strawhats.YourService -o ./your-service
cd ./your-service/src
dotnet build
dotnet run --project WebApi
```

The scaffolded service boots on `https://localhost:7257`. Hit `https://localhost:7257/health/live` to see it's alive. The Weather example endpoint is at `/api/v1/weather/forecasts/{city}` (alpha characters only).

## Companion CLI

For day-to-day work, install [`shf-cli`](https://github.com/StrawhatsCompany/shf-cli) and use it to scaffold features, endpoints, entities, providers, persistence projects, and migrations:

```powershell
dotnet tool install -g StrawhatsCompany.SHFramework.Cli

shf make:feature Weather/GetWeeklyForecast
shf make:endpoint Weather/GetWeeklyForecast
shf make:entity   Orders/Order --properties "CustomerName:string?,Amount:decimal"
shf make:provider Sms --first-driver Twilio
shf make:persistence postgres --connection-string "Host=localhost;Database=AppDb;Username=postgres;"
shf make:migration AddForecastTable
```

## Configuration

Non-secret defaults live in `appsettings.json`. Secrets — provider credentials, API keys, encryption keys, connection-string passwords — live in [user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (dev) or environment variables / a secret store (prod). See [`docs/SECRETS.md`](../docs/SECRETS.md).

```powershell
cd src/WebApi
dotnet user-secrets set "Mail:Password" "your-smtp-password"
```

## What's in the box

| Project | Role |
|---|---|
| `Domain` | Entities only — no dependencies |
| `Business` | CQRS handlers, contracts, OpenAPI registration, authentication contracts |
| `Business.Services` | Service implementations |
| `Providers.Mail` | Mail provider (Smtp), scaffold others with `shf make:provider` |
| `WebApi` | ASP.NET Core minimal API host |
| `tests/*.Tests` | xUnit + NSubstitute, one per source project |

Target framework: `net10.0`. See [`CLAUDE.md`](../CLAUDE.md) for the full conventions.

## Audit + skills

The repo ships two project-scoped Claude Code skills under `.claude/skills/`:
- `feature-audit` — bugs / security / performance lens.
- `framework-compliance` — CLAUDE.md rule compliance.

Both audit docs in [`docs/`](../docs/) record the findings that produced the current state.

## Build from source

```powershell
git clone git@github.com:StrawhatsCompany/sh-framework-template.git
cd sh-framework-template/src
dotnet build
dotnet test
dotnet pack StrawhatsCompany.SHFramework.Templates.csproj -c Release -o ../artifacts
```

## License

MIT — see [LICENSE](../LICENSE).
