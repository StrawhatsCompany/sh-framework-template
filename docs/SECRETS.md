# Secrets

Provider credentials, persistence connection strings, encryption keys, and third-party API keys are **never** committed to source control. They live in:

| Environment | Where secrets come from |
|---|---|
| Local development | [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (`dotnet user-secrets set`) |
| CI / production | Environment variables, Azure Key Vault, AWS Secrets Manager, etc. — never plaintext in `appsettings.Production.json` |

`appsettings.json` and `appsettings.Development.json` may contain **structure** and **non-secret defaults** (host names, ports, log levels, `UseSsl`, `FromAddress`). They must not contain anything that, if leaked, would let a third party act as your service.

## What counts as a secret

- Provider credentials: SMTP `Username`/`Password`, SendGrid API key, Twilio auth token, etc.
- Persistence connection strings that contain a password (`Server=...;Password=...`).
- Encryption keys, signing keys, JWT signing secrets.
- Any third-party API key or shared secret.

If a property name contains `Password`, `Secret`, `Key`, `ConnectionString`, `ApiKey`, or `Token`, assume it is a secret unless you can argue otherwise.

## How to set them locally

The `WebApi` project ships with a `<UserSecretsId>` so the standard tooling works out of the box.

```pwsh
# From the repo root
cd src/WebApi

# Set Mail credentials (the only secrets currently in scope)
dotnet user-secrets set "Mail:Username" "your-smtp-user"
dotnet user-secrets set "Mail:Password" "your-smtp-pass"

# Inspect what is set
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "Mail:Password"
```

User Secrets are stored in your OS user profile (on Windows: `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`). They are loaded automatically by the host when `ASPNETCORE_ENVIRONMENT=Development` — no `Program.cs` change is needed.

## How to set them in production

Use the platform-native mechanism. Examples:

```pwsh
# Environment variables (any host)
$env:Mail__Username = "..."
$env:Mail__Password = "..."

# Azure App Service / Container Apps configuration
az webapp config appsettings set --name <app> --settings Mail__Username=... Mail__Password=...

# Kubernetes
kubectl create secret generic mail-credentials \
  --from-literal=Mail__Username=... \
  --from-literal=Mail__Password=...
# Then reference it as env in the deployment.
```

Note the double underscore (`__`) — it is how .NET's environment-variable provider represents nested config keys (`Mail:Password` becomes `Mail__Password`).

## Current secrets in scope

| Key | Used by | Type |
|---|---|---|
| `Mail:Username` | `MailOptions` → `SmtpProvider.AuthenticateAsync` | optional — leave unset for unauthenticated relays (MailHog, dev) |
| `Mail:Password` | `MailOptions` → `SmtpProvider.AuthenticateAsync` | optional — required when `Username` is set |

Future providers and persistence integrations will extend this table. The rule applies to every one of them.

## Reviewing your own setup

Before pushing, sanity-check that no secret has leaked into a committed file:

```pwsh
git grep -E "(Password|ApiKey|ApiSecret|ConnectionString|Token)\s*[:=]" -- "*.json" "*.cs" "*.yaml"
```

A hit on a placeholder (`"smtp.example.com"`, `noreply@example.com`) is fine. A hit on a real-looking value is not.
