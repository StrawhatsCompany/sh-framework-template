using Business.Features.Mails.Send;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using WebApi.Common;

namespace WebApi.Endpoints.Mails;

public sealed class SendMailEndpoint : IEndpoint
{
    public static string Route => "api/v1/mails/send";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async ([FromBody] SendMailCommand command, [FromServices] IProjector projector, CancellationToken cancellationToken) =>
                (await projector.SendAsync(command, cancellationToken)).ToHttp())
            .WithTags("SendMail");
    }
}
