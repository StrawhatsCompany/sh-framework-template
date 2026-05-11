---
name: framework-compliance
description: Check whether the framework, a slice, or a feature follows the SH Framework rules documented in CLAUDE.md. Use this skill when the user asks to "check rules", "check compliance", "is this in the framework rules", "does this follow conventions", "audit conventions", or to verify a new vertical slice before merging. Distinct from `feature-audit` (which catches bugs/security/perf) — this one verifies architectural conventions only. Returns violations grouped by rule with file:line citations.
---

# Framework compliance check

The framework's rules live in `CLAUDE.md`. This skill walks a piece of code and checks each rule, returning the violations. **CLAUDE.md is authoritative — when in doubt, re-read it. This skill is a derivative.**

Pairs with `feature-audit` (bugs/security/perf) and `docs/ARCHITECTURE_AUDIT.md` (snapshot DRY/SOLID findings). Use all three for a complete review.

## Scope detection

The user typically asks one of three scopes:

| User says | Scope |
|---|---|
| "check the framework" / "audit the project" | Everything under `src/` |
| "check the X slice" / "is the X feature compliant" | `src/Business/Features/<Domain>/<Operation>/` + matching `src/WebApi/Endpoints/<Domain>/` + any provider/service touched |
| "check this file" (with a path) | That single file plus the slice it belongs to |

If unclear, ask **once** which scope. Then commit to it.

## Procedure

1. **Re-read `CLAUDE.md`.** Rules change. Don't rely on the version in this skill — pull from the source.
2. **Map the surface in scope.** List every file the check applies to.
3. **Walk the checklist below.** For each rule, decide: ✅ pass / ❌ fail / N/A. Cite file:line for failures.
4. **Cross-reference existing audit docs** (`docs/ARCHITECTURE_AUDIT.md`, `docs/QUALITY_AUDIT.md`) so a known finding isn't re-filed. Reference the existing ID (e.g. "tracked in #10").
5. **Output** a structured report — see template below. Default to inline chat output. Only write to a doc if the user asks.
6. **Open issues only for net-new material violations** (`gh issue create --repo StrawhatsCompany/sh-framework-template`). Add each to project board 5.

## Rules checklist

Grouped to match `CLAUDE.md`'s structure. Each rule has its CLAUDE.md anchor in parentheses.

### Layout (Layout, Core principles)

- [ ] **Target framework is `net10.0`** in every csproj.
- [ ] **Source lives under `src/`** in the canonical project layout (`Domain`, `Business`, `Business.Services`, `Persistence.PostgreSql`, `Providers.<Name>`, `WebApi`).
- [ ] **Tests live under `tests/<Name>.Tests`** with a 1:1 mapping to `src/<Name>`. Every source project has a matching test project.
- [ ] **Dependency direction** points inward toward `Domain`. `Domain` has zero project refs. `Business` references only `Domain`. Concrete impls (`Business.Services`, `Persistence.*`, `Providers.*`) reference `Business`. `WebApi` references the impls.

### Vertical slicing (Core principles)

- [ ] **Every feature lives in `src/Business/Features/<Domain>/<Operation>/`.** Request, handler, response, and validator are co-located in that folder — never split into `Commands/`, `Handlers/`, `Dtos/` siblings.
- [ ] **No cross-slice imports** between feature folders. A slice may use `Domain`, services, and providers — never another slice's internal types.
- [ ] **Feature folder name matches the operation.** `GetForecastsByCity`, not `Forecast` or `Query1`.

### Protocol-agnostic core (Core principles)

- [ ] **No HTTP types inside `Business`.** Greppable: `HttpContext`, `IFormFile`, `IActionResult`, `IResult`, `Microsoft.AspNetCore.*` in any `src/Business/**/*.cs` file is a violation. (Exceptions: `Business/Libraries/OpenApi/` is allowed to touch ASP.NET because that's its job.)
- [ ] **Handler is the contract; endpoint is a thin adapter.** Endpoint logic should be one expression: dispatch via `IProjector`, map result to HTTP.

### CQRS (Patterns → CQRS slice)

- [ ] **Commands extend `Request`** (no response) or **`Request<TResponse>`** (with response). Found in `SH.Framework.Library.Cqrs.Implementation`.
- [ ] **Handlers extend `RequestHandler<TRequest>`** or **`RequestHandler<TRequest, TResponse>`**.
- [ ] **Handler returns `Result` / `Result<T>`** — never raw values, never `throw` for expected failures.
- [ ] **Handlers are dispatched via `IProjector.SendAsync`** — endpoints never call services or providers directly.

### Minimal API endpoint (Patterns → Minimal API endpoint)

- [ ] **Endpoint implements `IEndpoint`** with a static `Route` and `Map(IEndpointRouteBuilder)`.
- [ ] **Endpoint is auto-discovered** via `app.MapEndpoints(Assembly.GetExecutingAssembly())` — no manual registration in `Program.cs`.
- [ ] **OpenAPI contract complete** — every endpoint declares **all** of: `.WithName`, `.WithSummary`, `.WithTags`, `.Produces<T>`, `.ProducesProblem(StatusCodes.Status400BadRequest)`. Missing any → violation.
- [ ] **Result is mapped to HTTP via `result.ToHttp()`** (from `WebApi.Common.ResultHttpExtensions`) — never `Results.Ok(result)` unconditionally, because that returns 200 on failure.
- [ ] **Route shape** follows `api/v{version}/<resource>/<operation>` (e.g. `api/v1/mails/send`). Routes without `api/v1/` prefix are violations.

### Provider pattern (Patterns → Provider pattern)

- [ ] **Contracts in `Business/Providers/<Name>/`** — interfaces, DTOs, credential types, result codes.
- [ ] **Implementations in `Providers.<Name>/`** — concrete provider classes (e.g. `SmtpProvider`), factory, registration.
- [ ] **Factory implements `IProviderFactory<TCredential, TProvider>`.**
- [ ] **Provider methods return `ProviderResult` / `ProviderResult<T>`** — `IsSuccess`, `Code`, `Errors`, with optional payloads.
- [ ] **Domain-specific result codes** subclass `ResultCode` (e.g. `MailProviderResultCode.NeedBody`).
- [ ] **No provider DTO leaks into `Domain`** — providers reference `Business`, not `Domain` directly.

### Service aggregator (Patterns → Service aggregator)

> ℹ️ The `IServices` aggregator is currently the documented convention but is on a deprecation path — see audit finding F5 (#10). Until that issue lands, the rule remains "handlers inject `IServices`". After #10 ships, switch this rule to "handlers inject only the services they need".

- [ ] **Domain services live under `Business.Services/<Domain>/`** with an interface in `Business/Services/<Domain>/`.
- [ ] **Services are registered in `RegisterBusinessServices.cs`** as `Scoped`.

### Registration convention (Patterns → Registration)

- [ ] **Every project exposes `Register<Name>.cs`** with a static `Add<Name>(this IServiceCollection)` method.
- [ ] **Web-facing projects also expose `Map<Name>(this WebApplication)`** when middleware/endpoint mapping is needed.
- [ ] **`Program.cs` chains them in order:** business contracts → impls → providers → `MapBusiness()` → `MapEndpoints(...)`.
- [ ] **New provider/service follows the same shape** — same naming, same chaining position.

### Testing (Testing)

- [ ] **One `<Name>.Tests` project per `src/<Name>`.** No missing test projects.
- [ ] **Test project references xUnit + NSubstitute** (NOT FluentAssertions — v8+ is paid).
- [ ] **Implicit usings include `Xunit` and `NSubstitute`** via `<Using Include=... />` in the csproj.
- [ ] **Test files mirror source namespace.** A test for `Business.Features.Weather.GetForecastsByCity` lives at `tests/Business.Tests/Features/Weather/GetForecastsByCityResponseTests.cs` and is in namespace `Business.Tests.Features.Weather`.
- [ ] **Handlers have unit tests** (mock dependencies). Endpoints get at least an integration test per route.
- [ ] **`dotnet test` is green** on the branch being reviewed.

### Conventions (Conventions)

- [ ] **`sealed` by default** on handlers, endpoints, DTOs. Non-sealed needs a comment explaining why subclassing is permitted.
- [ ] **Primary constructors for DI.** `public sealed class Foo(IBar bar) : ...` — not separate field + ctor.
- [ ] **`Result<T>` over exceptions** for expected failures. `throw` is reserved for programmer errors (preconditions, "should not happen").
- [ ] **One public type per file**, file name matches the type. Two `public sealed class` in one `.cs` file is a violation.
- [ ] **No "what does this do" comments.** Names carry the meaning. Comments are reserved for non-obvious *why*.
- [ ] **C# casing.** PascalCase for types, members, methods. camelCase only for locals/parameters.
- [ ] **Implicit usings respected.** No manual `using System.Linq;` / `using System.Collections.Generic;` in csprojs that have `<ImplicitUsings>enable</ImplicitUsings>`.
- [ ] **`CancellationToken` plumbed end-to-end** with parameter name `cancellationToken` or `ct` consistently within the slice (don't mix).
- [ ] **`required` / `init` for non-nullable properties** that have no constructor initialization — no CS8618 warnings on a clean build.

### Template hygiene (template.json + Dockerfile + compose)

- [ ] **`.template.config/template.json` exclude list** covers `bin/`, `obj/`, `.git/`, `.idea/`, `.vs/`, `.claude/`, `*.user`, `artifacts/`, `README.md`, dev appsettings.
- [ ] **`compose.yaml`** references only services with real Dockerfiles.
- [ ] **Dockerfile** uses an explicit non-root user (not `$APP_UID` env that may be unset).

### Secrets (CLAUDE.md → "No secrets in source")

- [ ] **`appsettings.json` and `appsettings.Development.json` contain no secrets.** Run `git grep -E "(Password|ApiKey|ApiSecret|ConnectionString|Token|Secret|SigningKey)" -- "*.json"` — every hit must be a structural key with a placeholder or empty value, never a real credential.
- [ ] **`WebApi.csproj` has a `<UserSecretsId>`.** Without it `dotnet user-secrets set` fails silently.
- [ ] **Options classes for secrets** mark the secret properties nullable (`string? Password { get; init; }`) so the type system accepts the "not set in dev" path. Any property named `Password`, `Secret`, `Key`, `ConnectionString`, `ApiKey`, or `Token` is suspect — verify its values come from user-secrets / env vars.
- [ ] **`docs/SECRETS.md` lists every secret in scope** with a one-liner `dotnet user-secrets set` example. When a new secret is added, the doc gets a new row.

## Output format

```markdown
## Framework compliance — <scope> — <date>

### ❌ Violations
- **<rule area> — <rule title>** `<file>:<line>` — <one sentence>. **Fix:** <one sentence>. (tracked: #<n> if applicable)

### ⚠️ Deprecated convention
- <rule that is being phased out — flag but don't file>

### ✅ Pass
- <count> of <total> rules pass.

### N/A
- <rules that don't apply to this scope>
```

Keep the report tight. The user reads diffs and CLAUDE.md — your job is to point at the gap, not re-teach the rule.

## When NOT to run

- The change is documentation-only — no code conventions to verify.
- The change is a test-only addition — run the testing rules only, skip the others.
- The user is mid-implementation and asked for help building, not reviewing.

## Companion skills

- `feature-audit` — bugs, security, performance (different lens, same scope mechanics).
- `/security-review` — built-in deep security pass; use when the change touches auth, secrets, or external surface.
