using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class StatCard : CardPanel
{
    private readonly Label _titleLabel = new() { AutoSize = false };
    private readonly Label _valueLabel = new() { AutoSize = false, Dock = DockStyle.Fill };
    private bool _isHovered;

    public StatCard()
    {
        ApplyMetrics();
        _titleLabel.Dock = DockStyle.Top;
        Controls.Add(_valueLabel);
        Controls.Add(_titleLabel);
        ApplyLocal();
        ThemeManager.Instance.Attach(this, (_, _) => ApplyLocal());
        LocalizationService.Instance.LanguageChanged += (_, _) => ApplyLocal();
        UiScale.Attach(this, (_, _) => { ApplyMetrics(); ApplyLocal(); });

        MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
        _titleLabel.MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        _titleLabel.MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
        _valueLabel.MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        _valueLabel.MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
    }

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = LocalizationService.Instance.IsRtl ? value : value.ToUpperInvariant();
    }

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
        var isRtl = LocalizationService.Instance.IsRtl;
        _titleLabel.Font = UiMetrics.Meta;
        _titleLabel.ForeColor = c.TextMuted;
        _titleLabel.BackColor = c.Surface;
        _titleLabel.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _valueLabel.Font = UiMetrics.StatValue;
        _valueLabel.ForeColor = c.TextPrimary;
        _valueLabel.BackColor = c.Surface;
        _valueLabel.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_isHovered)
        {
            var c = ThemeManager.Instance.Current;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var hoverPen = new Pen(c.AccentSoft, 1f);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            DrawingUtil.DrawRounded(g, hoverPen, rect, CornerRadius);
        }
    }
}
