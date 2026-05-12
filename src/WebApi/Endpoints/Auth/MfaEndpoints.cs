using Business.Features.Auth.MfaTotp;
using Business.Features.Auth.MfaVerify;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Auth;

public sealed class MfaVerifyEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/verify";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async (
                [FromBody] MfaVerifyCommand cmd, HttpContext http,
                [FromServices] IProjector projector, CancellationToken ct = default) =>
            {
                cmd.Ip ??= http.Connection.RemoteIpAddress?.ToString();
                return (await projector.SendAsync(cmd, ct)).ToHttp();
            })
        .WithName("MfaVerify")
        .WithSummary("Complete a half-authenticated login by verifying an MFA challenge")
        .WithDescription("Pre-auth endpoint. Caller submits the challenge id from the login response and the code from their authenticator (or email/SMS code in later channels). Returns the full access + refresh token pair on success.")
        .WithTags("Auth / MFA")
        .AllowAnonymous()
        .Produces<Result<MfaVerifyResponse>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class EnrollTotpEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/totp/enroll";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new EnrollTotpCommand(), ct)).ToHttp())
        .WithName("EnrollTotp")
        .WithSummary("Begin TOTP enrollment — returns the shared secret and otpauth URI")
        .WithDescription("Authenticated user generates a fresh TOTP secret. The factor lands in PendingEnrollment status; the user must then confirm a code via /mfa/totp/confirm to flip the factor to Active.")
        .WithTags("Auth / MFA")
        .RequireAuthorization()
        .Produces<Result<EnrollTotpResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class ConfirmTotpEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/totp/confirm";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromBody] ConfirmTotpCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("ConfirmTotp")
        .WithSummary("Confirm a TOTP enrollment by submitting a code from the authenticator")
        .WithTags("Auth / MFA")
        .RequireAuthorization()
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class DisableTotpEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/totp/{factorId:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid factorId, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new DisableTotpCommand { FactorId = factorId }, ct)).ToHttp())
        .WithName("DisableTotp")
        .WithSummary("Disable a TOTP factor (soft delete)")
        .WithTags("Auth / MFA")
        .RequireAuthorization()
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}
