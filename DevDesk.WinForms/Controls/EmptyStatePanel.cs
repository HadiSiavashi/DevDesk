using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class EmptyStatePanel : Panel
{
    private readonly Label _label = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 10F)
    };

    public EmptyStatePanel()
    {
        Dock = DockStyle.Fill;
        _label.Text = LocalizationService.Instance.Get("common.noData");
        Controls.Add(_label);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public string Message { get => _label.Text; set => _label.Text = value; }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.Background;
        _label.ForeColor = c.TextMuted;
    }
}
