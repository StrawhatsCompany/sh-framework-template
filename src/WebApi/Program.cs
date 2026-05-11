using System.Reflection;
using System.Threading.RateLimiting;
using Business.Libraries.Authentication;
using Business.Libraries.Authentication.ApiKey;
using Business.Libraries.Authentication.Authorization;
using Business.Libraries.Authentication.Jwt;
using Business.Libraries.Authentication.Mfa;
using Business.Providers.Mail;
using Business.Services;
using Caching.InMemory;
using Microsoft.AspNetCore.RateLimiting;
using Providers.Mail;
using SH.Framework.Library.AspNetCore;
using WebApi.Common;

var builder = WebApplication.CreateBuilder(args);

// Mail provider infrastructure is wired in even though no public endpoint currently uses it —
// internal handlers inject `IProviderFactory<MailProviderCredential, IMailProvider>` and
// `IOptions<MailOptions>` to send transactional email. SMTP credentials are read from user-secrets
// (dev) or env vars / secret store (prod) per `docs/SECRETS.md`; only structure lives here.
builder.Services
    .Configure<MailOptions>(builder.Configuration.GetSection(MailOptions.SectionName))
    .AddBusiness()
    .AddBusinessServices()
    .AddMailProvider()
    .AddInMemoryCaching(builder.Configuration);

// Authentication + authorization. JWT + API key are built-in; SSO is opt-in per provider
// (register your ISsoProvider implementations and add `.AddSso(...)` here). MFA orchestrator
// is wired; consumers register IMfaChannel implementations. AddAuthorizationModel registers
// every permission the application checks — add new ones here so typos fail loudly.
builder.Services.AddAuth(builder.Configuration, auth =>
{
    auth.AddJwt();
    auth.AddApiKey();
    auth.AddMfa();
    auth.AddAuthorizationModel(perms =>
    {
        // Register every permission the app guards with [HasPermission] / RequirePermission.
        // Example seeds:
        // perms.Add("weather.read", "orders.read", "orders.write");
    });
});

// Global exception handling — unhandled throws become ProblemDetails with a correlation id.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health checks — `/health/live` (process up) and `/health/ready` (DI healthy + tagged ready checks).
// Add per-service checks with `.AddCheck<TCheck>(name, tags: ["ready"])`.
builder.Services.AddHealthChecks();

// Rate limiting — fixed-window 100/minute keyed on the remote IP. Tune per service.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// CORS — registered with no origins by default. Each service narrows this for its UI clients via
// `appsettings` or programmatic `WithOrigins(...)`. Empty default means no cross-origin requests
// succeed; safer than `AllowAnyOrigin`.
builder.Services.AddCors();

var app = builder.Build();

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapBusiness();
app.MapEndpoints(Assembly.GetExecutingAssembly());

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();

// Exposes the auto-generated `Program` class to test projects so they can
// bootstrap the app via `WebApplicationFactory<Program>`.
public partial class Program { }
