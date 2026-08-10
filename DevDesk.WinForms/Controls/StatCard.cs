using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class StatCard : Panel
{
    private readonly Label _titleLabel = new() { AutoSize = false, Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 8.5F) };
    private readonly Label _valueLabel = new() { AutoSize = false, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };

    public StatCard()
    {
        Padding = new Padding(12);
        Height = 80;
        Width = 160;
        Controls.Add(_valueLabel);
        Controls.Add(_titleLabel);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public string Title { get => _titleLabel.Text; set => _titleLabel.Text = value; }
    public string Value { get => _valueLabel.Text; set => _valueLabel.Text = value; }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.Surface;
        _titleLabel.ForeColor = c.TextMuted;
        _valueLabel.ForeColor = c.Accent;
    }
}
