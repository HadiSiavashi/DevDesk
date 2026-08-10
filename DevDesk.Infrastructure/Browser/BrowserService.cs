using System.Diagnostics;
using DevDesk.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DevDesk.Infrastructure.Browser;

public sealed class BrowserService(ILogger<BrowserService> logger) : IBrowserService
{
    public bool IsValidHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
               || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    public void OpenUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        if (!IsValidHttpUrl(url))
            throw new ArgumentException("Only http and https URLs are allowed.", nameof(url));

        var uri = new Uri(url.Trim(), UriKind.Absolute);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open URL in the default browser.");
            throw new InvalidOperationException("Unable to open the URL in the default browser.", ex);
        }
    }
}
