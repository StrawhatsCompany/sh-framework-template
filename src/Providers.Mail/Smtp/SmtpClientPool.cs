using System.Collections.Concurrent;
using Business.Providers.Mail;
using MailKit.Net.Smtp;
using MimeKit;

namespace Providers.Mail.Smtp;

/// <summary>
/// One <see cref="SmtpClient"/> per (host, port, ssl, user) tuple, serialized through a
/// <see cref="SemaphoreSlim"/> because MailKit's <c>SmtpClient</c> is not thread-safe.
/// Registered as a singleton so the connection survives across requests.
/// </summary>
internal sealed class SmtpClientPool : ISmtpClientPool, IDisposable
{
    private readonly ConcurrentDictionary<string, PoolEntry> _entries = new();
    private bool _disposed;

    public async Task SendAsync(MailProviderCredential credential, MimeMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entry = _entries.GetOrAdd(KeyFor(credential), _ => new PoolEntry());

        await entry.Lock.WaitAsync(cancellationToken);
        try
        {
            if (!entry.Client.IsConnected)
            {
                await entry.Client.ConnectAsync(credential.HostName, credential.Port, credential.UseSsl, cancellationToken);
            }

            if (!entry.Client.IsAuthenticated && !string.IsNullOrEmpty(credential.UserName))
            {
                await entry.Client.AuthenticateAsync(credential.UserName, credential.Password ?? string.Empty, cancellationToken);
            }

            await entry.Client.SendAsync(message, cancellationToken);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var entry in _entries.Values)
        {
            try
            {
                if (entry.Client.IsConnected)
                {
                    entry.Client.Disconnect(quit: true);
                }
            }
            catch
            {
                // best-effort; broker may be unreachable on shutdown
            }
            entry.Client.Dispose();
            entry.Lock.Dispose();
        }
        _entries.Clear();
    }

    internal static string KeyFor(MailProviderCredential credential) =>
        $"{credential.HostName}|{credential.Port}|{credential.UseSsl}|{credential.UserName ?? ""}";

    private sealed class PoolEntry
    {
        public SmtpClient Client { get; } = new SmtpClient();
        public SemaphoreSlim Lock { get; } = new(initialCount: 1, maxCount: 1);
    }
}
