using System.Reflection;
using Business.Providers.Mail;
using Business.Services;
using Providers.Mail;
using SH.Framework.Library.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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
