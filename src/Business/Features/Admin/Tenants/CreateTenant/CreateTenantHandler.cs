using System.Text.RegularExpressions;
using Business.Common;
using Business.Identity;
using Domain.Entities.Identity;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Admin.Tenants.CreateTenant;

public sealed partial class CreateTenantHandler(ITenantStore tenants, IUserContext userCtx)
    : RequestHandler<CreateTenantCommand, CreateTenantResponse>
{
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$", RegexOptions.Compiled)]
    private static partial Regex SlugPattern();

    public override async Task<Result<CreateTenantResponse>> HandleAsync(
        CreateTenantCommand request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();

        if (!SlugPattern().IsMatch(slug))
        {
            return Result.Failure<CreateTenantResponse>(IdentityResultCode.TenantSlugInvalid);
        }

        if (await tenants.FindBySlugAsync(slug, cancellationToken) is not null)
        {
            return Result.Failure<CreateTenantResponse>(IdentityResultCode.TenantSlugAlreadyExists);
        }

        var now = DateTime.UtcNow;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? slug : request.DisplayName.Trim(),
            Status = TenantStatus.Active,
            CreatedAt = now,
            CreatedBy = userCtx.UserId,
        };

        await tenants.AddAsync(tenant, cancellationToken);

        return Result.Success(new CreateTenantResponse { Tenant = TenantDto.From(tenant) });
    }
}
