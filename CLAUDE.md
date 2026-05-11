this # CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

A `dotnet new` template (`shf`) that scaffolds a Strawhats framework service. The `src/` directory **is** the template source — changes here become part of the generated project.

## Commands

```powershell
# Build everything
dotnet build

# Run the API
dotnet run --project WebApi

# Pack the template as a NuGet package
dotnet pack .\StrawhatsCompany.SHFramework.Templates.csproj -c Release -o .\artifacts\packages

# Install the template locally for testing
dotnet new install .

# Scaffold a new service from the template
dotnet new shf -n Strawhats.Authentication -o .\authentication\src
```

## Architecture

Target framework: **net10.0**. The solution follows Clean Architecture with these projects:

| Project | Role |
|---|---|
| `Domain` | Entities only — no dependencies |
| `Business` | CQRS handlers, service/provider interfaces, OpenAPI registration |
| `Business.Services` | Service implementations (depends on `Business`) |
| `Persistence.PostgreSql` | PostgreSQL persistence (depends on `Business`) |
| `Providers.Mail` | Mail provider implementations (depends on `Business`) |
| `WebApi` | ASP.NET Core minimal API host |

## Key patterns

### CQRS (`SH.Framework.Library.Cqrs.Implementation`)

Commands and queries live in `Business/Features/<Domain>/<Operation>/`. A handler pair looks like:

```csharp
// Query (no response body for commands)
public sealed class MyCommand : Request { }

// Handler
public sealed class MyHandler(...) : RequestHandler<MyCommand>
{
    public override async Task<Result> HandleAsync(MyCommand request, CancellationToken ct = default) { ... }
}

// Query with response
public sealed class MyQuery : Request { }
public sealed class MyQueryHandler(...) : RequestHandler<MyQuery, MyQueryResponse>
{
    public override async Task<Result<MyQueryResponse>> HandleAsync(MyQuery request, CancellationToken ct = default) { ... }
}
```

Handlers are dispatched via `IProjector.SendAsync(...)`, which is injected into endpoints from `SH.Framework.Library.AspNetCore`.

### Minimal API endpoints (`SH.Framework.Library.AspNetCore`)

Endpoints implement `IEndpoint` and are auto-discovered by `app.MapEndpoints(Assembly.GetExecutingAssembly())` in `Program.cs`. No manual registration is needed.

```csharp
public class MyEndpoint : IEndpoint
{
    public static string Route => "api/v1/resource";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async ([FromBody] MyCommand cmd, [FromServices] IProjector projector, CancellationToken ct) =>
            Results.Ok(await projector.SendAsync(cmd, ct)));
    }
}
```

### Provider pattern

External service integrations follow a factory pattern. The `Business` project owns only contracts and interfaces; concrete implementations live in their own provider project (e.g., `Providers.Mail`).

- `IProviderFactory<TCredential, TProvider>` — factory interface in `Business/Providers/`
- `ProviderCredential<TProviderType>` — base credential with host/port/auth fields
- `ProviderResult` / `ProviderResult<T>` — result wrapper with `IsSuccess`, `Code`, `Errors`, and optional request/response payloads
- `ResultCode` instances (e.g., `MailProviderResultCode`) group domain-specific result codes by category

### Service aggregator

`IServices` / `Services` aggregate all domain services so handlers receive a single dependency:

```csharp
public sealed class MyHandler(IServices services) : RequestHandler<MyQuery, MyResponse>
```

### Registration convention

Every project exposes a static extension method on `IServiceCollection` (and `WebApplication` where needed). `Program.cs` chains them:

```csharp
builder.Services.AddBusiness()
    .AddBusinessServices()
    .AddMailProvider();

app.MapBusiness();           // registers OpenAPI
app.MapEndpoints(assembly); // auto-discovers IEndpoint implementations
```

When adding a new provider project, follow the same pattern: create a `Register<Name>.cs` with `Add<Name>(this IServiceCollection)` and call it in `Program.cs`.