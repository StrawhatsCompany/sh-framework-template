---
name: feature-audit
description: Audit a feature (vertical slice) or the whole codebase for bugs, security issues, and bottlenecks. Use this skill when the user asks to "audit", "review", "check quality of", or "do a quality pass on" a feature, slice, endpoint, handler, or the project. Also use proactively after a new feature is added under `src/Business/Features/**` before it is merged. Produces a finding list with file:line citations, ranked 🔴 material / 🟡 cosmetic / ℹ️ note.
---

# Feature audit — bugs / security / bottlenecks

A repeatable checklist for auditing a vertical slice in this template. Run it on every feature before it merges. Re-run it on the whole `src/` when refactoring.

## Inputs

The user typically says one of:
- `audit the SendMail feature` → scope is `src/Business/Features/Mails/Send/` + matching endpoint + provider impl
- `audit the project` → scope is everything under `src/`
- `check this slice` (with a path) → scope is that path

If unclear, ask once which slice. Don't ask twice.

## Procedure

1. **Map the surface.** Find every file in scope:
   - Slice folder: `src/Business/Features/<Domain>/<Operation>/`
   - Matching endpoint: `src/WebApi/Endpoints/<Domain>/`
   - Services touched: `src/Business.Services/<Domain>/`
   - Providers touched: `src/Providers.<Name>/`
   - Test counterparts: `tests/<Name>.Tests/Features/<Domain>/`

2. **Walk the checklist below.** For each finding, record:
   - File and line range
   - Category: 🐛 Bug / 🔐 Security / 🐢 Bottleneck
   - Severity: 🔴 material / 🟡 cosmetic / ℹ️ note
   - One-sentence "why it matters"
   - One-sentence suggested fix

3. **Cross-reference existing audits.** Check `docs/ARCHITECTURE_AUDIT.md` and `docs/QUALITY_AUDIT.md` — don't re-file something already tracked. Reference the existing finding ID instead.

4. **Output.** Append findings to `docs/QUALITY_AUDIT.md` under a dated section, OR (if the user is just asking for a review pass) write the report inline as a chat response. Default: inline + ask if they want it filed.

5. **Open issues for 🔴 material findings only.** Use `gh issue create --repo StrawhatsCompany/sh-framework-template`. Add each issue to the project board (`gh project item-add 5 --owner StrawhatsCompany --url <url>`).

## Checklist

### 🐛 Bugs

- [ ] **Command/query has real fields.** No empty `Request` types with hardcoded handler data.
- [ ] **Handler returns `Result.Failure(...)` for every error path.** No `throw` for expected failures. No silent success.
- [ ] **Endpoint maps `Result` to HTTP status.** Failure path must NOT return 200 OK. Use `result.ToHttp()` (or equivalent) — see #9.
- [ ] **Validation runs before any state mutation.** Don't half-build messages, half-write to DB, then fail.
- [ ] **Enum defaults are explicit.** No reliance on `default(TEnum) == DesiredCase`. Set the discriminator explicitly.
- [ ] **One public type per file.** Split commands and handlers.
- [ ] **C# casing.** PascalCase for properties/types/methods. camelCase only for locals/parameters.
- [ ] **Unused parameters.** Either use them or remove them. Document if "wired but not consumed".
- [ ] **`CancellationToken` plumbed end-to-end.** Endpoint → handler → service → provider → external call.
- [ ] **`compose.yaml` / `Dockerfile` / `template.json`** still reference real paths and projects.

### 🔐 Security

- [ ] **No hardcoded PII or credentials.** No real emails, names, API keys, tokens, hosts, or passwords in source. Use `noreply@example.com` and `IOptions<T>`.
- [ ] **Endpoint declares its auth posture.** Either `.RequireAuthorization(...)` with a named policy, or `.AllowAnonymous()` with a comment saying why. Default for any side-effectful endpoint is **authorized**.
- [ ] **No raw payload serialization.** Don't `JsonSerializer.Serialize(request)` then attach to a result or log — leaks PII, tokens, attachment streams. If needed, log metadata only (count, length).
- [ ] **`AllowedHosts` is not `"*"` in any non-Development config.**
- [ ] **Rate limiting wired** for any endpoint that has a side effect on external systems or is anonymous.
- [ ] **Body size cap.** Endpoints accepting attachments / large bodies declare a `RequestSizeLimit` or rely on a Kestrel-level cap, not the unbounded default.
- [ ] **Secrets not in `appsettings.json`.** Use User Secrets / env vars / a secret store. `[JsonIgnore]` on `Password`/`ApiKey`/`ApiSecret` properties.
- [ ] **`.dockerignore` / `template.json`** exclude dev settings, `.vs/`, `.idea/`, `.claude/`, `*.user`, `appsettings.Development.json` (from prod image).
- [ ] **Dockerfile pins a non-root user explicitly** (not via `$APP_UID` env that may be unset).
- [ ] **HTTPS redirection + HSTS** for non-Development environments.
- [ ] **Input validation.** Email addresses, URLs, file types validated, not just trusted.
- [ ] **Path traversal.** When accepting filenames, reject `..`, absolute paths, NUL bytes.
- [ ] **No SQL string concatenation** in `Persistence.PostgreSql`. Parameterized queries only.

### 🐢 Bottlenecks

- [ ] **Connection pooling.** Don't `new` external clients (SMTP, HTTP, DB) per request when they support reuse. Use `IHttpClientFactory`, MailKit connection reuse, DB connection pooling.
- [ ] **`async` all the way down.** No `.Result` / `.Wait()` / `Task.Run` wrapping for I/O.
- [ ] **No service locator on hot paths.** `IServiceProvider.GetRequiredService` per call multiplies. Inject directly.
- [ ] **Materialization.** `ToList()` only when you actually need a list. Return `IReadOnlyList<T>` from queries; use arrays for fixed-size results.
- [ ] **Heavy work avoided on hot paths.** No JSON serialization, no reflection, no LINQ-over-LINQ in the common case.
- [ ] **Caching.** Deterministic queries (`GetForecastsByCity` for today) get `OutputCache` / `IMemoryCache`. Idempotent commands get an idempotency key.
- [ ] **Response compression / output caching** wired in `Program.cs` for the WebApi project.
- [ ] **Eager vs lazy iteration.** Don't enumerate the same `IEnumerable` twice; materialize once.
- [ ] **N+1 queries.** Persistence layer must batch.

## Output format

```markdown
## Audit: <feature name> — <date>

### Material 🔴
- **<id>** [<category>] `<file>:<line>` — <one sentence>. **Fix:** <one sentence>.

### Cosmetic 🟡
- ...

### Notes ℹ️
- ...

### Already tracked
- B2 → #9, F5 → #10, ...
```

## When NOT to run this

- The change is documentation-only (no code touched).
- The change is a test-only addition.
- A previous audit ran in the same conversation and the scope hasn't changed.
