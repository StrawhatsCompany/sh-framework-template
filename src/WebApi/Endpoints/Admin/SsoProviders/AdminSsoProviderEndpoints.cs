using Business.Authentication.Authorization;
using Business.Features.Admin.SsoProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.SsoProviders;

public sealed class ListSsoProvidersEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/sso-providers";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async ([FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new ListSsoProvidersQuery(), ct)).ToHttp())
        .WithName("ListSsoProviders").WithSummary("List SSO providers in the current tenant").WithTags("Admin / SSO")
        .RequirePermission("admin.sso-providers.read")
        .Produces<Result<ListSsoProvidersResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest).ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class GetSsoProviderEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/sso-providers/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new GetSsoProviderQuery { Id = id }, ct)).ToHttp())
        .WithName("GetSsoProvider").WithSummary("Get an SSO provider by id").WithTags("Admin / SSO")
        .RequirePermission("admin.sso-providers.read")
        .Produces<Result<GetSsoProviderResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class CreateSsoProviderEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/sso-providers";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromBody] CreateSsoProviderCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("CreateSsoProvider").WithSummary("Add an SSO provider to the tenant")
        .WithDescription("ClientSecret is encrypted at rest via ICredentialProtector. Endpoints (AuthorizationEndpoint / TokenEndpoint / JwksUri / Issuer) must be set; OIDC discovery auto-populate lands in a follow-up.")
        .WithTags("Admin / SSO")
        .RequirePermission("admin.sso-providers.write")
        .Produces<Result<CreateSsoProviderResponse>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class UpdateSsoProviderEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/sso-providers/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch(Route, async (Guid id, [FromBody] UpdateSsoProviderCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
        {
            cmd.Id = id;
            return (await projector.SendAsync(cmd, ct)).ToHttp();
        })
        .WithName("UpdateSsoProvider").WithSummary("Update an SSO provider (set ClientSecret to rotate)")
        .WithTags("Admin / SSO")
        .RequirePermission("admin.sso-providers.write")
        .Produces<Result<UpdateSsoProviderResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}

public sealed class DeleteSsoProviderEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/sso-providers/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new DeleteSsoProviderCommand { Id = id }, ct)).ToHttp())
        .WithName("DeleteSsoProvider").WithSummary("Soft-delete an SSO provider").WithTags("Admin / SSO")
        .RequirePermission("admin.sso-providers.write")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized).ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status400BadRequest);
}
