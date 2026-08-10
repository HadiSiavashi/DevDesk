using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class LoadingPanel : Panel
{
    private readonly Label _label = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Text = LocalizationService.Instance.Get("common.loading")
    };

    public LoadingPanel()
    {
        Dock = DockStyle.Fill;
        Controls.Add(_label);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.Background;
        _label.ForeColor = c.TextSecondary;
    }
}
