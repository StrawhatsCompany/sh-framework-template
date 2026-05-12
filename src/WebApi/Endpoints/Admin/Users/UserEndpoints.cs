using Business.Features.Admin.Users.CreateUser;
using Business.Features.Admin.Users.DeleteUser;
using Business.Features.Admin.Users.GetUser;
using Business.Features.Admin.Users.ListUsers;
using Business.Features.Admin.Users.SetUserRoles;
using Business.Features.Admin.Users.UpdateUser;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SH.Framework.Library.AspNetCore;
using SH.Framework.Library.Cqrs.Implementation;
using WebApi.Common;

namespace WebApi.Endpoints.Admin.Users;

// TODO(perms): every route here lands behind [HasPermission("admin.users.*")] once #75 brings authentication.

public sealed class ListUsersEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (
                [FromQuery] UserStatus? status,
                [FromServices] IProjector projector,
                CancellationToken ct = default) =>
            (await projector.SendAsync(new ListUsersQuery { Status = status }, ct)).ToHttp())
        .WithName("ListUsers").WithSummary("List users").WithTags("Admin / Users")
        .Produces<Result<ListUsersResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class GetUserEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new GetUserQuery { Id = id }, ct)).ToHttp())
        .WithName("GetUser").WithSummary("Get a user by id").WithTags("Admin / Users")
        .Produces<Result<GetUserResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class CreateUserEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Route, async ([FromBody] CreateUserCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(cmd, ct)).ToHttp())
        .WithName("CreateUser").WithSummary("Create a user").WithTags("Admin / Users")
        .Produces<Result<CreateUserResponse>>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class UpdateUserEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch(Route, async (Guid id, [FromBody] UpdateUserCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
        {
            cmd.Id = id;
            return (await projector.SendAsync(cmd, ct)).ToHttp();
        })
        .WithName("UpdateUser").WithSummary("Update a user")
        .WithDescription("Patch semantics: null fields are left unchanged. Changing Phone clears PhoneVerifiedAt.")
        .WithTags("Admin / Users")
        .Produces<Result<UpdateUserResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class DeleteUserEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users/{id:guid}";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete(Route, async (Guid id, [FromServices] IProjector projector, CancellationToken ct = default) =>
            (await projector.SendAsync(new DeleteUserCommand { Id = id }, ct)).ToHttp())
        .WithName("DeleteUser").WithSummary("Soft-delete a user").WithTags("Admin / Users")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}

public sealed class SetUserRolesEndpoint : IEndpoint
{
    public static string Route => "api/v1/admin/users/{id:guid}/roles";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut(Route, async (Guid id, [FromBody] SetUserRolesCommand cmd, [FromServices] IProjector projector, CancellationToken ct = default) =>
        {
            cmd.UserId = id;
            return (await projector.SendAsync(cmd, ct)).ToHttp();
        })
        .WithName("SetUserRoles").WithSummary("Replace the user's role assignments")
        .WithDescription("Body is the full list of role ids the user should have. Roles not in the list are unassigned.")
        .WithTags("Admin / Users")
        .Produces<Result>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
}
