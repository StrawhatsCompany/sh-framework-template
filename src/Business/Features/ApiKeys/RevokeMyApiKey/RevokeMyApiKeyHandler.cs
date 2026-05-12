using Business.Authentication;
using Business.Authentication.ApiKeys;
using Business.Common;
using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.ApiKeys.RevokeMyApiKey;

public sealed class RevokeMyApiKeyCommand : Request
{
    public Guid Id { get; set; }
}

public sealed class RevokeMyApiKeyHandler(
    IApiKeyStore apiKeys,
    ITenantContext tenantCtx,
    IUserContext userCtx)
    : RequestHandler<RevokeMyApiKeyCommand>
{
    public override async Task<Result> HandleAsync(RevokeMyApiKeyCommand request, CancellationToken cancellationToken = default)
    {
        if (tenantCtx.TenantId is not { } tenantId || userCtx.UserId is not { } userId)
        {
            return Result.Failure(AuthResultCode.InvalidCredentials);
        }

        var stored = await apiKeys.FindByIdAsync(tenantId, request.Id, cancellationToken);
        if (stored is null || stored.UserId != userId)
        {
            return Result.Failure(AuthResultCode.ApiKeyNotFound);
        }

        var ok = await apiKeys.RevokeAsync(tenantId, request.Id, userId, cancellationToken);
        return ok ? Result.Success() : Result.Failure(AuthResultCode.ApiKeyNotFound);
    }
}
