using Business.Common;

namespace Business.Identity;

internal static class TenantContextExtensions
{
    /// <summary>
    /// Reads <see cref="ITenantContext.TenantId"/>; throws an <see cref="InvalidOperationException"/>
    /// if it's null. Use only in handlers that should never run outside a tenant scope —
    /// admin endpoints are gated by <c>X-Tenant-Id</c> / the JWT <c>tid</c> claim.
    /// Endpoints/handlers that need to fail soft (return Result.Failure) should null-check
    /// directly and return <c>IdentityResultCode.TenantRequired</c>.
    /// </summary>
    public static Guid RequireTenant(this ITenantContext context) =>
        context.TenantId ?? throw new InvalidOperationException(
            "Tenant context is required for this operation. Authenticate with a tenant-scoped token or pass X-Tenant-Id.");
}
