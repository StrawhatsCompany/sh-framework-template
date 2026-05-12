using Business.Features.Auth.MfaEmail;
using Business.Features.Auth.MfaSms;
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

public sealed class EnrollEmailEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/email/enroll";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new EnrollEmailCommand(), ct)).ToHttp())
        .WithName("EnrollEmailMfa")
        .WithSummary("Enroll the caller's verified email as an MFA factor")
        .WithDescription("Requires User.EmailVerifiedAt to be set. The factor activates immediately — no separate confirm step, since the email itself was verified during signup or change-email.")
        .WithTags("Auth / MFA")
        .RequireAuthorization()
        .Produces<Result<EnrollEmailResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class DisableEmailEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/email/{factorId:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid factorId, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new DisableEmailCommand { FactorId = factorId }, ct)).ToHttp())
        .WithName("DisableEmailMfa")
        .WithSummary("Disable an email MFA factor (soft delete)")
        .WithTags("Auth / MFA")
        .RequireAuthorization()
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class EnrollSmsEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/sms/enroll";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromBody] EnrollSmsCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("EnrollSmsMfa")
        .WithSummary("Enroll an SMS MFA factor — dispatches a one-time confirm code")
        .WithDescription("Body: { phone }. The factor lands in PendingEnrollment; the user must POST the dispatched code to /mfa/sms/confirm to flip it Active. If the phone matches User.Phone, confirmation also stamps User.PhoneVerifiedAt.")
        .WithTags("Auth / MFA")
        .RequireAuthorization()
        .Produces<Result<EnrollSmsResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class ConfirmSmsEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/sms/confirm";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromBody] ConfirmSmsCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("ConfirmSmsMfa")
        .WithSummary("Confirm an SMS MFA enrollment by submitting the dispatched code")
        .WithTags("Auth / MFA")
        .RequireAuthorization()
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class DisableSmsEndpoint : IEndpoint
{
    public static string Route => "api/v1/auth/mfa/sms/{factorId:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid factorId, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new DisableSmsCommand { FactorId = factorId }, ct)).ToHttp())
        .WithName("DisableSmsMfa")
        .WithSummary("Disable an SMS MFA factor (soft delete)")
        .WithTags("Auth / MFA")
        .RequireAuthorization()
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}
