using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Users.CreateUser;

public sealed class CreateUserCommand : Request<CreateUserResponse>
{
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string? Phone { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Password { get; set; }
}

public sealed class CreateUserResponse
{
    public required UserDto User { get; init; }
}
