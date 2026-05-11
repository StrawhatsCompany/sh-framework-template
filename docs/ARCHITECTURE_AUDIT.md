# Architecture Audit — DRY / SOLID / Clean Architecture

Snapshot date: 2026-05-11. Branch: `docs/dry-solid-audit`. Scope: `src/Business/Features/**`, `src/WebApi/Endpoints/**`, provider/service registration, `Business.Services/**`, `Providers.Mail/**`.

Findings are ranked **🔴 material** (should be fixed before this template is used to scaffold real services), **🟡 cosmetic** (worth fixing, won't break anything), **ℹ️ note** (intentional or unclear — needs a decision).

---

## 🔴 Material findings

### F1 — `SendMailHandler` is a hardcoded demo, not a handler

**File:** `src/Business/Features/Mails/Send/SendMailCommand.cs:9-43`

- `SendMailCommand` (L9-11) is an empty `Request` — it carries no recipient, no subject, no body. The "command" can't actually command anything.
- The handler hardcodes the SMTP host (`localhost:1025`), the sender (`noreply@example.com`), the recipient (`recipient@example.com`), the subject, and the HTML body (L17-30).
- Credential `ProviderType` is never set (L17-21) — it relies on `default(MailProviderType)` happening to equal `Smtp`. Adding a new enum value would silently break this.
- SMTP credentials should come from configuration (`IOptions<MailProviderCredential>`), not a literal in business code.

**Principles violated:** SRP (handler doing config + transport selection), DIP (concrete config inside business layer), OCP (enum addition breaks behavior silently).

**Suggested refactor:**
- Move `SendMailCommand` fields → `From`, `To`, `Subject`, `Body`, etc.
- Bind credentials from `appsettings.json` via `MailOptions` and inject `IOptions<MailOptions>` (or pass through `IServices`).
- Always set `ProviderType` explicitly.

### F2 — Two public types in one file

**File:** `src/Business/Features/Mails/Send/SendMailCommand.cs`

Contains both `SendMailCommand` and `SendMailHandler`. CLAUDE.md convention: "Files match types. One public type per file."

**Suggested refactor:** Split into `SendMailCommand.cs` and `SendMailHandler.cs`. Keep them co-located in the slice folder.

### F3 — Endpoints don't follow OpenAPI conventions

**Files:** `src/WebApi/Endpoints/Weather/GetForecastsByCityEndpoint.cs:11-17`, `src/WebApi/Endpoints/Mails/SendMailEndpoint.cs:13-19`

Neither endpoint declares the full OpenAPI surface that CLAUDE.md now requires:

| Endpoint | `WithName` | `WithSummary` | `WithTags` | `Produces<T>` | `ProducesProblem` |
|---|---|---|---|---|---|
| `GetForecastsByCity` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `SendMail` | ❌ | ❌ | ✅ | ❌ | ❌ |

**Principles violated:** Inconsistency, missing contract for API consumers.

**Suggested refactor:** Add the missing metadata to each endpoint. Both routes also need response-type declarations for proper Swagger/Scalar UX.

### F4 — `SendMailEndpoint` returns 200 OK on failure

**File:** `src/WebApi/Endpoints/Mails/SendMailEndpoint.cs:13-17`

```csharp
var result = await projector.SendAsync(command, cancellationToken);
return Results.Ok(result);
```

When `result.IsSuccess == false`, this still returns HTTP 200 with the failure body embedded. Clients can't rely on status codes; tooling (retries, alerting, monitoring) breaks.

**Principles violated:** REST contract violation, leaks `Result<T>` envelope semantics into the wire.

**Suggested refactor:** Map `Result` → `IResult`. Pattern (one helper, reused everywhere):
```csharp
result.IsSuccess
    ? Results.Ok(result.Value)
    : Results.Problem(detail: ..., statusCode: ResultCodeToHttp(result.Code));
```
Centralize as `result.ToHttp()` extension in `SH.Framework.Library.AspNetCore`.

### F5 — `IServices` aggregator violates ISP and is a service locator

**File:** `src/Business.Services/Services.cs:6-9`

```csharp
public class Services(IServiceProvider sp): IServices
{
    public IForecastService Forecast => sp.GetRequiredService<IForecastService>();
}
```

- Resolves dependencies lazily through `IServiceProvider` — classic service locator anti-pattern.
- Every handler that takes `IServices` is now coupled to **every** registered service, even ones it doesn't use. `GetForecastByCityHandler` only needs `IForecastService` — making it depend on the whole aggregator means changes to unrelated services force-rebuild this handler.
- Mocking: tests for one handler must stub out the whole `IServices`.

**Principles violated:** ISP (fat interface), DIP (service locator hides dependencies).

**Suggested refactor:** Drop `IServices`. Each handler injects only the interfaces it needs:
```csharp
public sealed class GetForecastByCityHandler(IForecastService forecasts)
    : RequestHandler<GetForecastsByCityQuery, GetForecastsByCityResponse>
```
Update CLAUDE.md "Service aggregator" section to reflect this.

---

## 🟡 Cosmetic findings

### F6 — `ProviderCredential` builds with nullability warnings

**File:** `src/Business/Providers/ProviderCredential.cs:5,16`

Build emits CS8618 for `HostName` and `ProviderType` (non-nullable, no initializer, no `required`). Two warnings on a clean build is two warnings too many.

**Suggested refactor:** Add `required` modifier:
```csharp
public required string HostName { get; init; }
public required TProviderType ProviderType { get; init; }
```

### F7 — Endpoint classes not `sealed`

**Files:** `src/WebApi/Endpoints/Weather/GetForecastsByCityEndpoint.cs:7`, `src/WebApi/Endpoints/Mails/SendMailEndpoint.cs:7`

CLAUDE.md convention: handlers, endpoints, DTOs are `sealed` by default.

### F8 — Inconsistent route conventions

**Files:** `src/WebApi/Endpoints/Weather/GetForecastsByCityEndpoint.cs:19`, `src/WebApi/Endpoints/Mails/SendMailEndpoint.cs:9`

- `GetForecastsByCity` → `/weather/forecasts/{city:alpha}` (no `api/v1/` prefix)
- `SendMail` → `api/v1/mails/send`

Pick one. Recommend `api/v{version}/<resource>/<operation>` for all endpoints, and route prefixing via a route group in `MapBusiness`.

### F9 — Redundant `using`s in `ProviderResult.cs`

**File:** `src/Business/Providers/ProviderResult.cs:1-2`

`System.Collections.Generic` and `System.Linq` are already in implicit usings (csproj has `ImplicitUsings=enable`). Remove them.

### F10 — `attachment.contentType` field naming

**File:** `src/Providers.Mail/Smtp/SmtpProvider.cs:58`

Property accessed as `attachment.contentType` (lowercase `c`). C# convention is PascalCase. Verify and rename `MailAttachment.contentType` → `ContentType`.

### F11 — `SmtpProvider` validates body mid-build

**File:** `src/Providers.Mail/Smtp/SmtpProvider.cs:41-44`

Body presence is checked after `From`/`To`/`Cc`/`Bcc` are already added to the `MimeMessage`. Move validation up front so failure short-circuits cleanly.

### F12 — `SmtpProvider` returns asymmetric payloads

**File:** `src/Providers.Mail/Smtp/SmtpProvider.cs:71-73`

On success the result carries the request JSON but no response payload. The `Response` `("","SENT")` tuple also hardcodes empty id. Either capture the SMTP response (message-id from the server when available) or document why it's empty.

---

## ℹ️ Notes

### N1 — `ForecastService` ignores the `city` parameter

**File:** `src/Business.Services/Weather/ForecastService.cs:8-19`

The service generates random forecasts and never uses the `city` argument. Fine for a scaffold demo, but flag in the README so adopters aren't confused.

### N2 — `MailProviderResultCode.NeedBody` is the only domain-specific code

A single result code suggests the catalog is under-developed. Expect to grow this (auth failure, rate limit, transient failure, etc.) once a real provider is wired up.

### N3 — Provider factory registered as Singleton

**File:** `src/Providers.Mail/RegisterMailProvider.cs:11`

OK because `ProviderFactory.Create` is stateless and the resulting `SmtpProvider` carries its own credential. Document the contract: provider factories must be safe to register as Singleton.

---

## Follow-up

One GitHub issue opened per **material** finding (F1–F5). Cosmetic findings tracked in this doc only — fix opportunistically when the surrounding code is touched.
