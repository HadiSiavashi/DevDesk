using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class ToastHost : Panel
{
    private readonly System.Windows.Forms.Timer _hideTimer = new();
    private string _title = "";
    private string _body = "";
    private bool _error;
    private bool _visibleToast;

    public ToastHost()
    {
        Tag = "no-theme";
        Size = new Size(UiMetrics.ToastWidth, 0);
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        DrawingUtil.EnableDoubleBuffer(this);
        Cursor = Cursors.Hand;
        _hideTimer.Tick += (_, _) => BeginHide();
        Click += (_, _) => BeginHide();
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    public void ShowToast(string message, bool isError = false)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ShowToast(message, isError));
            return;
        }

        var parts = message.Split('\n', 2);
        _title = parts[0];
        _body = parts.Length > 1 ? parts[1] : "";
        _error = isError;
        _visibleToast = true;
        Height = string.IsNullOrEmpty(_body) ? 56 : 72;
        if (Parent is not null)
        {
            Left = Math.Max(8, Parent.ClientSize.Width - Width - 16);
            Top = Math.Max(8, Parent.ClientSize.Height - Height - 16);
            BringToFront();
        }
        Invalidate();
        _hideTimer.Stop();
        _hideTimer.Interval = UiMetrics.ToastMs;
        _hideTimer.Start();
    }

    private void BeginHide()
    {
        _hideTimer.Stop();
        _visibleToast = false;
        Height = 0;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (!_visibleToast || Height < 8) return;
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var bg = new SolidBrush(c.Overlay))
            DrawingUtil.FillRounded(g, bg, rect, UiMetrics.RadiusMd);
        using (var pen = new Pen(c.Border))
            DrawingUtil.DrawRounded(g, pen, rect, UiMetrics.RadiusMd);
        using (var accent = new Pen(_error ? c.Error : c.Accent, 2))
            g.DrawLine(accent, 1, 10, 1, Height - 10);

        UiIcons.Draw(g, _error ? "error" : "task_alt", new Rectangle(12, 16, 20, 20), _error ? c.Error : c.AccentSoft);
        TextRenderer.DrawText(g, _title, UiMetrics.SectionTitle, new Rectangle(40, 10, Width - 70, 22), c.TextPrimary);
        if (!string.IsNullOrEmpty(_body))
            TextRenderer.DrawText(g, _body, UiMetrics.Body, new Rectangle(40, 32, Width - 70, 28), c.TextSecondary, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        UiIcons.Draw(g, "close", new Rectangle(Width - 28, 12, 16, 16), c.TextMuted);
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (Parent is not null)
            Parent.Resize += (_, _) =>
            {
                Left = Math.Max(8, Parent.ClientSize.Width - Width - 16);
                Top = Math.Max(8, Parent.ClientSize.Height - Math.Max(Height, 1) - 16);
            };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hideTimer.Dispose();
        base.Dispose(disposing);
    }
}
