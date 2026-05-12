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

## Supported matrix

This template is a [bill of materials](https://en.wikipedia.org/wiki/Bill_of_materials) for the SH Framework family — each release pins a vetted set of sibling library versions. Pin the template version in your service, and you get a tested combination. The actual versions live in `src/Directory.Build.props` (MSBuild properties), referenced from the consuming csprojs via `Version="$(SHxxxVersion)"`. The table below is the human-readable changelog.

| Template version | `SH.Framework.Library.Cqrs` | `SH.Framework.Library.Cqrs.Implementation` | `SH.Framework.Library.Cqrs.Implementation.EntityFrameworkCore` | `SH.Framework.Library.AspNetCore` | `StrawhatsCompany.SHFramework.Cli` (recommended) |
|---|---|---|---|---|---|
| **3.10.0** (current) | 3.0.1 | 3.0.8 | 10.0.3 | 1.0.0 | 0.4.1 |
| 3.9.0 | 3.0.1 | 3.0.8 | 10.0.3 | 1.0.0 | 0.4.1 |
| 3.8.0 | 3.0.1 | 3.0.8 | 10.0.3 | 1.0.0 | 0.4.1 |
| 3.7.0 | 3.0.1 | 3.0.8 | 10.0.3 | 1.0.0 | 0.4.1 |
| 3.6.0 | 3.0.1 | 3.0.8 | 10.0.3 | 1.0.0 | 0.4.1 |
| 3.5.0 | 3.0.1 | 3.0.8 | 10.0.3 | 1.0.0 | 0.4.1 |
| 3.4.0 | 3.0.1 | 3.0.8 | 10.0.3 | 1.0.0 | 0.4.1 |

The first four columns are package references resolved at restore time. The CLI is a `dotnet tool` — install separately with `dotnet tool install -g StrawhatsCompany.SHFramework.Cli`. The matrix lists the CLI version this template release was tested against; newer minor/patch CLI versions are expected to remain compatible.

**Rule of thumb:** when a sibling library bumps and we want to adopt it, bump `SHxxxVersion` in `src/Directory.Build.props` AND bump this template's `<PackageVersion>` (patch for a lib fix, minor for a lib feature). Add a new row to this table.

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
