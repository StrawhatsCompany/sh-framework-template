using Business.Features.Mails.Send;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;

namespace WebApi.Endpoints.Mails;

public class SendMailEndpoint : IEndpoint
{
    public static string Route => "api/v1/mails/send";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async ([FromBody] SendMailCommand command, [FromServices] IProjector projector, CancellationToken cancellationToken) => {
            var result = await projector.SendAsync(command, cancellationToken);
            
            return Results.Ok(result);
        })
            .WithTags("SendMail");
    }
}
