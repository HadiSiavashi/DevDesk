using System.Globalization;

namespace DevDesk.WinForms.Localization;

public sealed class LocalizationService
{
    public static LocalizationService Instance { get; } = new();

    private CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

    public CultureInfo CurrentCulture
    {
        get => _culture;
        set
        {
            _culture = value;
            CultureInfo.CurrentUICulture = value;
            CultureInfo.CurrentCulture = value;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsRtl => _culture.TwoLetterISOLanguageName.Equals("fa", StringComparison.OrdinalIgnoreCase);

    public event EventHandler? LanguageChanged;

    public void SetLanguage(string cultureName)
    {
        CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
    }

    public string Get(string key)
    {
        var dict = _culture.TwoLetterISOLanguageName.Equals("fa", StringComparison.OrdinalIgnoreCase)
            ? UiCatalog.Persian
            : UiCatalog.English;

        return dict.TryGetValue(key, out var value) ? value : key;
    }

    public void ApplyRtl(Control control)
    {
        control.RightToLeft = IsRtl ? RightToLeft.Yes : RightToLeft.No;
        if (control is Form form)
            form.RightToLeftLayout = IsRtl;
        foreach (Control child in control.Controls)
            ApplyRtl(child);
    }
}
