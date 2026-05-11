# CLAUDE.md

Guidance for Claude Code when working in this repo. Keep edits terse — tokens matter.

## What this is

A `dotnet new` template (id `shf`) that scaffolds a Strawhats framework service. `src/` **is** the template body — every change ships to generated projects. `tests/` mirrors `src/` 1:1.

## Layout

| Path | Role | Depends on |
|---|---|---|
| `src/Domain` | Entities only | — |
| `src/Business` | CQRS handlers, contracts, OpenAPI registration | Domain |
| `src/Business.Services` | Service implementations | Business |
| `src/Persistence.PostgreSql` | PostgreSQL persistence | Business |
| `src/Providers.Mail` | Mail provider impls | Business |
| `src/WebApi` | ASP.NET Core minimal API host | Business + impls |
| `tests/<Name>.Tests` | xUnit, one per source project | matching `src/<Name>` |

Target: **net10.0**. Clean Architecture — deps point inward to `Domain`.

## Commands

```powershell
dotnet build                                           # build all
dotnet test                                            # run all tests
dotnet run --project src/WebApi                        # run API
dotnet pack StrawhatsCompany.SHFramework.Templates.csproj -c Release -o artifacts/packages
dotnet new install .                                   # install template locally
dotnet new shf -n MyService -o out/MyService           # scaffold a service
```

## Core principles

- **Vertical slicing.** Every feature lives in `src/Business/Features/<Domain>/<Operation>/` — request, handler, response, validator are co-located. Never split a feature across "Commands/", "Handlers/", "Dtos/" folders. Add files to the slice, not to layers.
- **Protocol-agnostic core.** WebApi could be REST, GraphQL, or gRPC. We ship REST only, but treat the handler as the contract and the endpoint as a thin adapter. No HTTP types (`HttpContext`, `IFormFile`, `IActionResult`) inside `Business`.
- **CQRS via `IProjector`.** Endpoints never call services directly — they dispatch a `Request` through `IProjector.SendAsync`.
- **DRY / SOLID.** Shared infra goes in `SH.Framework.Library.*`. Don't fork it inline.
- **No secrets in source.** Provider credentials, persistence connection strings, encryption keys, and API keys never live in `appsettings.json`. Dev → `dotnet user-secrets set` (the `WebApi` project carries `<UserSecretsId>shframework-webapi</UserSecretsId>`). Prod → env vars or a secret store. `appsettings.json` holds structure and non-secret defaults only. See `docs/SECRETS.md`. Any property named `Password`, `Secret`, `Key`, `ConnectionString`, `ApiKey`, or `Token` is assumed to be a secret.

## Patterns

### CQRS slice (`SH.Framework.Library.Cqrs.Implementation`)

```csharp
// src/Business/Features/Weather/GetForecastsByCity/
public sealed class GetForecastsByCityQuery : Request { public string City { get; init; } = ""; }

public sealed class GetForecastsByCityResponse { /* ... */ }

public sealed class GetForecastByCityHandler(IServices services)
    : RequestHandler<GetForecastsByCityQuery, GetForecastsByCityResponse>
{
    public override async Task<Result<GetForecastsByCityResponse>> HandleAsync(
        GetForecastsByCityQuery req, CancellationToken ct = default)
    {
        var forecasts = await services.Forecast.GetForecastAsync(req.City, ct);
        return Result.Success(GetForecastsByCityResponse.Create(req.City, forecasts));
    }
}
```

Commands use `RequestHandler<TRequest>` (no response). Always return `Result` / `Result<T>`.

### Minimal API endpoint (`SH.Framework.Library.AspNetCore`)

Auto-discovered by `app.MapEndpoints(Assembly.GetExecutingAssembly())`.

```csharp
public class GetForecastsByCityEndpoint : IEndpoint
{
    public static string Route => "api/v1/weather/forecasts/{city:alpha}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (string city, [FromServices] IProjector projector, CancellationToken ct) =>
                Results.Ok(await projector.SendAsync(new GetForecastsByCityQuery { City = city }, ct)))
            .WithName("GetForecastsByCity")
            .WithSummary("Get weather forecasts for a city")
            .WithTags("Weather")
            .Produces<GetForecastsByCityResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
}
```

OpenAPI contract for every endpoint: `.WithName`, `.WithSummary`, `.WithTags`, `.Produces<T>`, `.ProducesProblem(...)`. No exceptions.

### Provider pattern

External integrations follow a factory. `Business` owns contracts only; impls live in `Providers.<Name>`.

- `IProviderFactory<TCredential, TProvider>` — `src/Business/Providers/`
- `ProviderCredential<TProviderType>` — host/port/auth base
- `ProviderResult` / `ProviderResult<T>` — `IsSuccess`, `Code`, `Errors`, optional payloads
- `ResultCode` subclasses (e.g. `MailProviderResultCode`) group domain codes

### Service aggregator

`IServices` / `Services` exposes every domain service so handlers take one dep:

```csharp
public sealed class MyHandler(IServices services) : RequestHandler<MyQuery, MyResponse>
```

### Registration

Each project ships a `Register<Name>.cs` exposing `Add<Name>(this IServiceCollection)` (and `Map<Name>(this WebApplication)` when needed). `Program.cs` chains them:

```csharp
builder.Services.AddBusiness().AddBusinessServices().AddMailProvider();
app.MapBusiness();                                  // OpenAPI
app.MapEndpoints(Assembly.GetExecutingAssembly());  // IEndpoint discovery
```

New provider → new `Register<Name>.cs` → chain in `Program.cs`. Same shape every time.

## Testing

- One `tests/<Name>.Tests` project per `src/<Name>`. Mirror the namespace.
- xUnit + NSubstitute. No mocking of concrete classes — depend on interfaces.
- Test handlers (not endpoints) for business logic. Endpoints get one integration test per route.
- `dotnet test` must be green before any PR merges.

## Conventions

- `sealed` by default for handlers, endpoints, DTOs.
- Primary constructors for DI.
- `Result<T>` over exceptions for expected failures.
- Files match types. One public type per file.
- No comments explaining *what* — names do that. Comments only for non-obvious *why*.
