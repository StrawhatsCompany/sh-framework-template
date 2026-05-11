using System.Reflection;
using Business.Providers.Mail;
using Business.Services;
using Providers.Mail;
using SH.Framework.Library.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Mail provider infrastructure is wired in even though no public endpoint currently uses it —
// internal handlers inject `IProviderFactory<MailProviderCredential, IMailProvider>` and
// `IOptions<MailOptions>` to send transactional email. SMTP credentials are read from user-secrets
// (dev) or env vars / secret store (prod) per `docs/SECRETS.md`; only structure lives here.
builder.Services
    .Configure<MailOptions>(builder.Configuration.GetSection(MailOptions.SectionName))
    .AddBusiness()
    .AddBusinessServices()
    .AddMailProvider();

var app = builder.Build();

app.MapBusiness();
app.MapEndpoints(Assembly.GetExecutingAssembly());

app.UseHttpsRedirection();


app.Run();

// Exposes the auto-generated `Program` class to test projects so they can
// bootstrap the app via `WebApplicationFactory<Program>`.
public partial class Program { }
