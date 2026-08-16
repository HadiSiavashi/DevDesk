using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class StatCard : CardPanel
{
    private readonly Label _titleLabel = new() { AutoSize = false, Height = 16 };
    private readonly Label _valueLabel = new() { AutoSize = false, Dock = DockStyle.Fill };

    public StatCard()
    {
        Padding = new Padding(UiMetrics.Space12);
        Height = 72;
        MinimumSize = new Size(120, 72);
        _titleLabel.Dock = DockStyle.Top;
        Controls.Add(_valueLabel);
        Controls.Add(_titleLabel);
        ApplyLocal();
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyLocal();
    }

    public string Title { get => _titleLabel.Text; set => _titleLabel.Text = value.ToUpperInvariant(); }
    public string Value { get => _valueLabel.Text; set => _valueLabel.Text = value; }

    private void ApplyLocal()
    {
        var c = ThemeManager.Instance.Current;
        _titleLabel.Font = UiMetrics.Meta;
        _titleLabel.ForeColor = c.TextMuted;
        _titleLabel.BackColor = c.Surface;
        _valueLabel.Font = UiMetrics.StatValue;
        _valueLabel.ForeColor = c.TextPrimary;
        _valueLabel.BackColor = c.Surface;
        _valueLabel.TextAlign = ContentAlignment.MiddleLeft;
    }
}
