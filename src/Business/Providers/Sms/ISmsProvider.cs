using Business.Providers.Sms.Contracts;

namespace Business.Providers.Sms;

public interface ISmsProvider
{
    Task<ProviderResult<SendSmsContract.Response>> SendAsync(
        SendSmsContract.Request request, CancellationToken cancellationToken = default);
}
