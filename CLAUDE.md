# CLAUDE.md

Guidance for Claude Code when working in this repo. Keep edits terse — tokens matter.

## What this is

A `dotnet new` template (id `shf`) that scaffolds a Strawhats framework service. `src/` **is** the template body — every change ships to generated projects. `tests/` mirrors `src/` 1:1.

## Layout

| Path | Role | Depends on |
|---|---|---|
| `src/Domain` | Entities only | — |
| `src/Business` | CQRS handlers, contracts, OpenAPI registration, authentication contracts | Domain |
| `src/Business.Services` | Service implementations | Business |
| `src/Providers.Mail` | Mail provider impls | Business |
| `src/Caching.InMemory` | In-memory cache provider | Business |
| `src/WebApi` | ASP.NET Core minimal API host | Business + impls |
| `tests/<Name>.Tests` | xUnit, one per source project | matching `src/<Name>` |

Persistence projects are not in the default scaffold — add one with `shf make:persistence <postgres|sqlserver|sqlite>`.

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

public sealed class GetForecastByCityHandler(IForecastService forecasts)
    : RequestHandler<GetForecastsByCityQuery, GetForecastsByCityResponse>
{
    public override async Task<Result<GetForecastsByCityResponse>> HandleAsync(
        GetForecastsByCityQuery req, CancellationToken ct = default)
    {
        var forecast = await forecasts.GetForecastAsync(req.City, ct);
        return Result.Success(GetForecastsByCityResponse.Create(req.City, forecast));
    }
}
```

Commands use `RequestHandler<TRequest>` (no response). Always return `Result` / `Result<T>`.

### Minimal API endpoint (`SH.Framework.Library.AspNetCore`)

Auto-discovered by `app.MapEndpoints(Assembly.GetExecutingAssembly())`.

```csharp
public sealed class GetForecastsByCityEndpoint : IEndpoint
{
    public static string Route => "api/v1/weather/forecasts/{city:alpha}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (string city, [FromServices] IProjector projector, CancellationToken ct) =>
                (await projector.SendAsync(new GetForecastsByCityQuery { City = city }, ct)).ToHttp())
            .WithName("GetForecastsByCity")
            .WithSummary("Get weather forecasts for a city")
            .WithDescription("Optional longer prose. Use for non-obvious behavior or constraints.")
            .WithTags("Weather")
            .Produces<Result<GetForecastsByCityResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
}
```

OpenAPI contract every endpoint must declare:
- `.WithName(...)` — operation id, matches the slice name (no `Endpoint` suffix).
- `.WithSummary(...)` — short, one-liner; this is what Swagger shows in the list view.
- `.WithTags(...)` — group by domain (`Weather`, `Mails`, etc.) for the UI.
- `.Produces<Result<TResponse>>(StatusCodes.Status200OK)` — the **wire shape** is the `Result<T>` envelope, not the unwrapped data; Swagger should not lie about that.
- `.ProducesValidationProblem()` — for `Result.Failure(... , errors)` mapped through `result.ToHttp()`.
- `.ProducesProblem(StatusCodes.Status400BadRequest)` — for `Result.Failure(...)` with no errors.
- `.ProducesProblem(StatusCodes.Status500InternalServerError)` — for `ResultCode.Exception` failures.

`.WithDescription` is optional but encouraged when the route has non-obvious behavior. No exceptions to the rest.

### Provider pattern

External integrations follow a factory. `Business` owns contracts only; impls live in `Providers.<Name>`.

- `IProviderFactory<TCredential, TProvider>` — `src/Business/Providers/`
- `ProviderCredential<TProviderType>` — host/port/auth base
- `ProviderResult` / `ProviderResult<T>` — `IsSuccess`, `Code`, `Errors`, optional payloads
- `ResultCode` subclasses (e.g. `MailProviderResultCode`) group domain codes

### Service injection

Handlers inject **only the services they actually use** — never a god-aggregator that exposes the whole catalog. Each domain service has its own interface in `src/Business/Services/<Domain>/` and an implementation in `src/Business.Services/<Domain>/`. The handler takes them as constructor params via primary constructor:

```csharp
public sealed class MyHandler(IForecastService forecasts, IOrderRepository orders)
    : RequestHandler<MyQuery, MyResponse>
```

This keeps handlers honest about their dependencies — adding a new service to the catalog doesn't ripple into every handler, mocking takes only what the test needs, and the type system surfaces over-injection (too many ctor params → split the handler).

### Registration

Each project ships a `Register<Name>.cs` exposing `Add<Name>(this IServiceCollection)` (and `Map<Name>(this WebApplication)` when needed). `Program.cs` chains them:

```csharp
builder.Services.AddBusiness().AddBusinessServices().AddMailProvider().AddInMemoryCaching(builder.Configuration);
app.MapBusiness();                                  // OpenAPI
app.MapEndpoints(Assembly.GetExecutingAssembly());  // IEndpoint discovery
```

New provider → new `Register<Name>.cs` → chain in `Program.cs`. Same shape every time.

### Caching

Multi-provider, parallel to Mail. Contracts in `src/Business/Caching/`:

- `ICacheProvider` — `GetAsync<T>` / `SetAsync<T>` / `ExistsAsync` / `RemoveAsync`.
- `CacheCredential : ProviderCredential<CacheProviderType>` — same factory pattern as mail.
- `CacheOptions` — bound from `Caching` section (`DefaultTtl`, `KeyPrefix`).

Default impl: `src/Caching.InMemory` (wraps `IMemoryCache`). Future impls follow the `Caching.<Provider>` naming (`Caching.Redis`, `Caching.Memcached`, etc.) — register via `.Add<Provider>Caching(IConfiguration)`. Generate one with `shf make:caching <name>`.

## Authentication & authorization

Wired in `Program.cs` via the fluent `AddAuth` builder.

```csharp
builder.Services.AddAuth(builder.Configuration, auth =>
{
    auth.AddJwt();        // Authentication:Jwt — SigningKey from user-secrets
    auth.AddApiKey();     // consumer registers IApiKeyValidator
    auth.AddSso(/* ISsoProvider[] */);
    auth.AddMfa();        // consumer registers IMfaChannel + (production) IMfaCodeStore
    auth.AddAuthorizationModel(perms =>
    {
        perms.Add("orders.read", "orders.write");
    });
});
```

### Schemes

- **JWT bearer** (built-in). `JwtOptions` bound from `Authentication:Jwt`. `IJwtTokenIssuer` mints tokens from a claim set — populate role + `permissions` claims at issue time. `JwtOptionsValidator` fails fast at startup if `SigningKey` is missing or < 256 bits.
- **API key** (built-in). `X-Api-Key` header (configurable). Consumer implements `IApiKeyValidator` to map keys → claims.
- **SSO** (contract only). `ISsoProvider` lets consumers wire Google / GitHub / Entra / etc. via their own libraries. Each provider attaches its own ASP.NET handler.
- **MFA** (contract + default orchestrator). `IMfaChannel` per channel (SMS / email / TOTP), `IMfaCodeStore` for persistence (`InMemoryMfaCodeStore` is a dev-only default; **register a real store for production**). `IMfaChallengeIssuer` generates a hashed N-digit code, dispatches via the channel, persists, then verifies on submission with constant-time compare.

### Authorization model

Permission-based on top of ASP.NET policies:

- `AddAuthorizationModel(perms => perms.Add(...))` registers every permission the app knows about. Endpoints can only assert permissions in the catalog — typos throw at policy build.
- Role → permissions map lives in `Authorization:Roles` (config or `IPermissionResolver` override).
- Mark endpoints with `[HasPermission("orders.read")]` or `.RequirePermission("orders.read")` (minimal-API).
- Custom claim type for direct grants: `AuthorizationClaims.Permission` (`permissions`).

`docs/SECRETS.md` is the index for every secret in scope (Mail credentials, JWT signing key, etc.).

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
