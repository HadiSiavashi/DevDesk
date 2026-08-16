using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class ErrorPanel : Panel
{
    private readonly Label _message = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ModernButton _retry = new() { Text = LocalizationService.Instance.Get("common.retry"), Width = 88, Height = 28, Variant = ButtonVariant.Outline };
    private Func<Task>? _retryAction;

    public ErrorPanel()
    {
        Dock = DockStyle.Fill;
        Tag = "no-theme";
        Padding = new Padding(16);
        _retry.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _retry.Click += async (_, _) =>
        {
            if (_retryAction is not null) await _retryAction();
        };
        Controls.Add(_retry);
        Controls.Add(_message);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        Resize += (_, _) => _retry.Location = new Point(Width - 120, 16);
        ApplyTheme();
    }

    public void SetError(Exception ex, Func<Task>? retry)
    {
        _message.Text = ex.Message;
        _retryAction = retry;
        _retry.Visible = retry is not null;
    }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.Background;
        _message.ForeColor = c.Error;
        _message.Font = UiMetrics.Body;
        _message.Padding = new Padding(36, 0, 100, 0);
        _retry.Text = LocalizationService.Instance.Get("common.retry");
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = new Rectangle(16, 12, Width - 33, 48);
        using var bg = new SolidBrush(DrawingUtil.WithAlpha(c.Error, 30));
        DrawingUtil.FillRounded(g, bg, rect, UiMetrics.RadiusMd);
        using var pen = new Pen(c.ErrorContainer);
        DrawingUtil.DrawRounded(g, pen, rect, UiMetrics.RadiusMd);
        UiIcons.Draw(g, "error", new Rectangle(28, 24, 20, 20), c.Error);
    }
}
