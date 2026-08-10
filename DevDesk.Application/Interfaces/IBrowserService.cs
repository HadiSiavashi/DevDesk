namespace DevDesk.Application.Interfaces;

public interface IBrowserService
{
    /// <summary>
    /// Opens a URL in the default browser. Only http/https URLs are allowed.
    /// </summary>
    void OpenUrl(string url);

    /// <summary>
    /// Returns true when the URL uses http or https.
    /// </summary>
    bool IsValidHttpUrl(string? url);
}
