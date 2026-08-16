using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class EmptyStatePanel : Panel
{
    private readonly Label _label = new()
    {
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Fill
    };

    public EmptyStatePanel()
    {
        Dock = DockStyle.Fill;
        Tag = "no-theme";
        Padding = new Padding(24);
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
        _label.Font = UiMetrics.Body;
        _label.BackColor = c.Background;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = Rectangle.Inflate(ClientRectangle, -24, -24);
        using var pen = new Pen(c.Border) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        DrawingUtil.DrawRounded(g, pen, rect, UiMetrics.RadiusMd);
        UiIcons.Draw(g, "check_circle", new Rectangle(Width / 2 - 12, Height / 2 - 40, 24, 24), c.TextMuted);
    }
}
