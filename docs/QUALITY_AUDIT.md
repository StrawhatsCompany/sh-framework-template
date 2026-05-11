# Quality Audit — Bugs / Security / Bottlenecks

Snapshot date: 2026-05-11. Branch: `docs/quality-audit-and-skill`. Scope: full `src/` tree + Dockerfile + compose + settings.

Pairs with `docs/ARCHITECTURE_AUDIT.md` (which covered DRY/SOLID). This one covers: **correctness**, **security**, **performance**. Same legend — 🔴 material, 🟡 cosmetic, ℹ️ note.

---

## 🐛 Bugs (correctness)

### B1 🔴 — `compose.yaml` references a project that doesn't exist

**File:** `src/compose.yaml:6`

```yaml
webapplication1:
  build:
    dockerfile: WebApplication1/Dockerfile     # <-- WebApplication1 folder doesn't exist
```

Leftover from a `dotnet new` scaffold. `docker compose build` will fail for that service. **Effect:** template ships broken Docker tooling.

**Fix:** remove the `webapplication1` service entry. Keep only `webapi`.

### B2 🔴 — `SendMailEndpoint` returns HTTP 200 on failure

**File:** `src/WebApi/Endpoints/Mails/SendMailEndpoint.cs:13-17`

Already filed as #9 (audit F4). Listed here too because it's a correctness defect, not just style.

### B3 🔴 — `SmtpProvider` validates body after partial message build

**File:** `src/Providers.Mail/Smtp/SmtpProvider.cs:41-44`

By the time `HasBody` is checked, `From`/`To`/`Cc`/`Bcc` already mutated the `MimeMessage`. If body is missing on a request that also has invalid recipients, the recipient error never surfaces — body wins. Hidden ordering dependency.

**Fix:** validate the entire request up front, return early before any `MimeMessage` mutation.

### B4 🟡 — `ProviderType` not set on credential; relies on `default(MailProviderType) == Smtp`

**File:** `src/Business/Features/Mails/Send/SendMailCommand.cs:17-21`

Already noted as #6 (F1). Listed again because the failure mode is silent: add a new enum member with a smaller numeric value and the factory routes the wrong way.

### B5 🟡 — `MailAttachment.contentType` violates C# casing

**File:** `src/Business/Providers/Mail/Dtos/MailAttachment.cs:5`

```csharp
public record MailAttachment(string FileName, ContentType contentType, Stream File);
```

`contentType` should be `ContentType` (PascalCase). Roslyn analyzers will flag this; downstream code already accesses it as `attachment.contentType` (SmtpProvider:58) — fix both.

### B6 🟡 — `ForecastService` ignores the `city` argument

**File:** `src/Business.Services/Weather/ForecastService.cs:8`

`city` is unused. The endpoint accepts `{city:alpha}` but every call returns the same shape of random data. Either remove the parameter (template demo doesn't filter) or document that the parameter is wired but not consumed.

### B7 🟡 — `template.json` exclude list is incomplete

**File:** `src/.template.config/template.json:28-37`

Excludes `bin/`, `obj/`, `.git/`, `.idea/`, `artifacts/`, `README.md`, `*.DotSettings.user`. Missing:

- `.vs/` (Visual Studio cache, will pollute scaffolded projects)
- `.claude/` (Claude Code settings — local-only)
- `**/*.user` (Visual Studio per-user files, including `WebApi.csproj.user`)
- `compose.yaml` reference to `WebApplication1` (or fix B1 first)

---

## 🔐 Security

### S1 🔴 — Hardcoded personal emails in source code

**File:** `src/Business/Features/Mails/Send/SendMailCommand.cs:24-27`

The handler used to hardcode two personal `MailAddress` instances (a sender and a recipient) inside `SendMailHandler.HandleAsync`. Every service scaffolded from `dotnet new shf` ships with these as the default sender/recipient. **Effect:** PII leak, plus a real risk that someone deploys the template "as is" and starts sending production traffic to/from these inboxes.

**Fix:** remove the hardcoded data (already tracked in #6). In the interim, replace the addresses with obvious dummies like `noreply@example.com` and `recipient@example.com`.

### S2 🔴 — No authentication on mail send endpoint

**File:** `src/WebApi/Endpoints/Mails/SendMailEndpoint.cs:13-17`

`POST api/v1/mails/send` is anonymous. Once the template is used to scaffold a real service and connected to a real SMTP relay, this is an **open mail relay**. Anyone with network reach can send mail through it.

**Fix:** require an `[Authorize]` policy by default for any endpoint that has a side effect on external systems. Add an explicit `.AllowAnonymous()` opt-out for the ones that should be public. Document the convention in CLAUDE.md.

### S3 🔴 — `JsonSerializer.Serialize(request)` writes full payload to result

**File:** `src/Providers.Mail/Smtp/SmtpProvider.cs:73`

```csharp
return ProviderResult
    .Success(...)
    .WithRequestJson(JsonSerializer.Serialize(request));
```

The serialized `Request` contains every recipient, the full subject, the full body, and (because `MailAttachment.File` is a `Stream`) potentially a non-serializable or huge attachment payload. If `ProviderResult` is logged anywhere (default ASP.NET error pipeline, OpenTelemetry, etc.) this leaks PII and arbitrary file content into log sinks.

**Fix:**
- Don't auto-serialize sensitive request bodies. Capture only metadata (recipient count, subject length, attachment count).
- If full-payload logging is wanted, gate it behind a `LogPayloads` config flag, opt-in per environment.
- Either way, `Stream` should not be hit by `System.Text.Json` — it will either fail or serialize the stream's internal state.

### S4 🟡 — `AllowedHosts: "*"` accepts any Host header

**File:** `src/WebApi/appsettings.json:8`

`"*"` is the framework default for development. In production this allows host header spoofing → cache poisoning / password-reset link rewriting. Templates should set a concrete value (`"AllowedHosts": "localhost"` in dev, real domain in prod overrides).

### S5 🟡 — No rate limiting, HSTS, CORS, or input size limits

**File:** `src/WebApi/Program.cs:1-20`

The pipeline is `AddBusiness().AddBusinessServices().AddMailProvider() → MapBusiness → MapEndpoints → UseHttpsRedirection`. Missing baseline middleware:

- `app.UseHsts()` (production)
- `app.UseRateLimiter()` or `AddRateLimiter()` with a sensible default (e.g. 100 req/min/IP)
- `app.UseCors(...)` if any browser client will hit this
- `MaxRequestBodySize` cap on Kestrel (mail attachments could be huge)
- `IFormFeature` size limits if file upload is added later

**Fix:** add a `Register<Security>` extension in `SH.Framework.Library.AspNetCore` that wires all of these with sensible defaults and a config-driven override.

### S6 🟡 — `ProviderCredential.Password` etc. stored as plain `string`

**File:** `src/Business/Providers/ProviderCredential.cs:7-10`

Strings live in the managed heap until GC, are immutable, and may end up in heap dumps / crash dumps. For secrets, prefer `SecureString` (still has caveats) or, better, never hold the raw secret in a long-lived object — fetch from the secret store at call time.

**Fix (pragmatic):** at minimum, mark these properties so they don't get serialized by accident (`[JsonIgnore]`) and don't get logged by structured loggers (`[LogMasked]` if using Serilog).

### S7 🟡 — `appsettings.Development.json` not in `.dockerignore` exclude path

**File:** `src/.dockerignore`

Verify that dev-only settings (and any future `appsettings.Local.json`) don't ship into the production image. Today the `COPY . .` step in the Dockerfile happily includes them.

### S8 ℹ️ — Dockerfile uses `USER $APP_UID`

**File:** `src/WebApi/Dockerfile:2`

The official `mcr.microsoft.com/dotnet/aspnet:10.0` image sets `APP_UID=1654`. If a future contributor swaps the base image, `$APP_UID` may be empty and the container falls back to root. Pin explicitly: `USER 1654` or use the named user the base image provides.

---

## 🐢 Bottlenecks (performance)

### P1 🔴 — `SmtpProvider` creates a new `SmtpClient` per send

**File:** `src/Providers.Mail/Smtp/SmtpProvider.cs:64-69`

```csharp
using var client = new SmtpClient();
await client.ConnectAsync(...);
await client.SendAsync(...);
await client.DisconnectAsync(true);
```

Every send: TCP handshake → TLS handshake (when enabled) → AUTH → send → disconnect. At more than ~10 sends/sec this saturates the SMTP relay's connection limits and adds 100-500ms per call. The MailKit team's recommendation is to keep a connection open for batch sends.

**Fix:** pool the connection per `MailProviderCredential` (e.g. `ConcurrentDictionary<MailProviderCredential, Lazy<SmtpClient>>` with idle-eviction) or accept a list of messages and send them in one session.

### P2 🟡 — `IServices` aggregator calls `GetRequiredService` per property access

**File:** `src/Business.Services/Services.cs:8`

```csharp
public IForecastService Forecast => sp.GetRequiredService<IForecastService>();
```

Each handler invocation resolves the service through the container's dictionary. Negligible per call (microseconds) but multiplies across handlers. Also makes flame graphs noisier than needed.

**Fix:** Already tracked for removal in #10 (drop the aggregator entirely).

### P3 🟡 — `JsonSerializer.Serialize` on hot path of every mail send

**File:** `src/Providers.Mail/Smtp/SmtpProvider.cs:73`

See S3. On top of being a security issue, it's a perf issue: serializing the entire request (including streams and attachment metadata) for every send is wasted work when most environments will never inspect the payload.

### P4 🟡 — `ForecastService` materializes the list eagerly with two enumerations

**File:** `src/Business.Services/Weather/ForecastService.cs:10-17`

`Enumerable.Range(...).Select(...).ToList()` then returns the list. Not a bottleneck at n=5, but the pattern propagates to scaffolded services. Better to return `IReadOnlyList<Forecast>` (which `ToList` already satisfies) and use array allocation for fixed-size sequences: `new Forecast[5]`.

### P5 🟡 — No response compression, no output caching

**File:** `src/WebApi/Program.cs`

Forecast endpoint returns small JSON; for larger responses, `AddResponseCompression()` would help. `AddOutputCache()` for the forecast query (it's deterministic for a city + day) would eliminate repeat compute.

### P6 ℹ️ — Provider factory registered as Singleton

Singleton + per-call `new SmtpProvider(...)` is fine, but document the contract: provider factories must be safe to use from many threads concurrently. Without that contract, a future contributor may add mutable state and create a subtle race.

---

## Follow-ups

Material findings → tracking issues filed. Cosmetic ones → fix opportunistically. See `docs/ARCHITECTURE_AUDIT.md` for overlapping findings (B2, B4, S1, P2 are already tracked).
