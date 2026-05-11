using System.Reflection;
using Business.Services;
using Providers.Mail;
using SH.Framework.Library.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBusiness()
    .AddBusinessServices()
    .AddMailProvider();

var app = builder.Build();

app.MapBusiness();
app.MapEndpoints(Assembly.GetExecutingAssembly());

app.UseHttpsRedirection();


app.Run();