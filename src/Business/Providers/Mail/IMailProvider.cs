using Business.Providers.Mail.Contracts;

namespace Business.Providers.Mail;

public interface IMailProvider
{
    public Task<ProviderResult<SendMailContract.Response>> SendAsync(SendMailContract.Request request, CancellationToken cancellationToken = default);
}
