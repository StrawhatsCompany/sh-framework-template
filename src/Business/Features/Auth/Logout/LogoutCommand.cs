using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Features.Auth.Logout;

public sealed class LogoutCommand : Request
{
    public Guid SessionId { get; set; }
}
