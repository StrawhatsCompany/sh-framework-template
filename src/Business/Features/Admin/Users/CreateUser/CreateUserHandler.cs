using System.Text.RegularExpressions;
using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Users.CreateUser;

public sealed partial class CreateUserHandler(
    IUserStore users,
    IPasswordHasher hasher,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<CreateUserCommand, CreateUserResponse>
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[A-Za-z0-9._-]{2,64}$", RegexOptions.Compiled)]
    private static partial Regex UsernamePattern();

    public override async Task<Result<CreateUserResponse>> HandleAsync(
        CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId)
        {
            return Result.Failure<CreateUserResponse>(IdentityResultCode.TenantRequired);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim();

        if (!EmailPattern().IsMatch(email))
        {
            return Result.Failure<CreateUserResponse>(IdentityResultCode.UserEmailInvalid);
        }
        if (!UsernamePattern().IsMatch(username))
        {
            return Result.Failure<CreateUserResponse>(IdentityResultCode.UserUsernameInvalid);
        }
        if (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 12)
        {
            return Result.Failure<CreateUserResponse>(IdentityResultCode.UserPasswordTooWeak);
        }
        if (await users.FindByEmailAsync(tenantId, email, cancellationToken) is not null)
        {
            return Result.Failure<CreateUserResponse>(IdentityResultCode.UserEmailAlreadyExists);
        }
        if (await users.FindByUsernameAsync(tenantId, username, cancellationToken) is not null)
        {
            return Result.Failure<CreateUserResponse>(IdentityResultCode.UserUsernameAlreadyExists);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            Username = username,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? username : request.DisplayName.Trim(),
            PasswordHash = string.IsNullOrEmpty(request.Password) ? null : hasher.Hash(request.Password),
            Status = UserStatus.PendingVerification,
            CreatedAt = now,
            CreatedBy = userCtx.UserId,
        };

        await users.AddAsync(user, cancellationToken);

        return Result.Success(new CreateUserResponse { User = UserDto.From(user) });
    }
}
