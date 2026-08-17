using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class StatCard : CardPanel
{
    private readonly Label _titleLabel = new() { AutoSize = false };
    private readonly Label _valueLabel = new() { AutoSize = false, Dock = DockStyle.Fill };

    public StatCard()
    {
        ApplyMetrics();
        _titleLabel.Dock = DockStyle.Top;
        Controls.Add(_valueLabel);
        Controls.Add(_titleLabel);
        ApplyLocal();
        ThemeManager.Instance.Attach(this, (_, _) => ApplyLocal());
        UiScale.Attach(this, (_, _) => { ApplyMetrics(); ApplyLocal(); });
    }

    public string Title { get => _titleLabel.Text; set => _titleLabel.Text = value.ToUpperInvariant(); }
    public string Value { get => _valueLabel.Text; set => _valueLabel.Text = value; }

    private void ApplyMetrics()
    {
        Padding = new Padding(UiMetrics.Space12);
        Height = UiMetrics.StatCardHeight;
        MinimumSize = new Size(UiScale.Px(140), UiMetrics.StatCardHeight);
        _titleLabel.Height = UiMetrics.LineMeta;
    }

    private void ApplyLocal()
    {
        var c = ThemeManager.Instance.Current;
        _titleLabel.Font = UiMetrics.Meta;
        _titleLabel.ForeColor = c.TextMuted;
        _titleLabel.BackColor = c.Surface;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _valueLabel.Font = UiMetrics.StatValue;
        _valueLabel.ForeColor = c.TextPrimary;
        _valueLabel.BackColor = c.Surface;
        _valueLabel.TextAlign = ContentAlignment.MiddleLeft;
    }
}
