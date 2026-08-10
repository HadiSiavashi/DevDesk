namespace DevDesk.WinForms.Services;

public static class ClipboardHelper
{
    public static bool TrySetText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (Exception)
        {
            try
            {
                Clipboard.SetDataObject(text, copy: true, retryTimes: 5, retryDelay: 50);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
