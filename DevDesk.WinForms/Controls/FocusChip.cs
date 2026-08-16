using DevDesk.Application.Dtos;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class FocusChip : Control
{
    public event EventHandler? StopClicked;

    private string _label = "DEEP WORK";
    private string _time = "";
    private bool _paused;

    public FocusChip()
    {
        Height = 28;
        Width = 220;
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
        MouseClick += (s, e) =>
        {
            if (new Rectangle(Width - 26, 6, 16, 16).Contains(e.Location))
                StopClicked?.Invoke(this, EventArgs.Empty);
        };
    }

    public void Bind(FocusSessionDto? session)
    {
        Visible = session is { IsActive: true };
        if (session is not { IsActive: true }) return;
        _paused = session.IsPaused;
        _label = session.IsPaused ? "PAUSED" : "DEEP WORK";
        var now = DateTime.UtcNow;
        var end = session.EndedAt ?? now;
        var secs = (int)(end - session.StartedAt).TotalSeconds - session.PausedAccumulatedSeconds;
        if (session.IsPaused && session.PausedAt is DateTime p)
            secs -= (int)(now - p).TotalSeconds;
        secs = Math.Max(0, secs);
        var ts = TimeSpan.FromSeconds(secs);
        _time = ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        var name = session.TaskTitle ?? session.ProjectName;
        if (!string.IsNullOrEmpty(name))
            _label = name.Length > 18 ? name[..17] + "…" : name;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var bg = new SolidBrush(c.SelectedBg))
            DrawingUtil.FillRounded(g, bg, rect, UiMetrics.RadiusSm);
        using (var pen = new Pen(c.Accent, 2))
            g.DrawLine(pen, 1, 4, 1, Height - 5);

        using var dot = new SolidBrush(_paused ? c.Tertiary : c.Error);
        g.FillEllipse(dot, 10, Height / 2 - 3, 6, 6);
        TextRenderer.DrawText(g, _label.ToUpperInvariant(), UiMetrics.Meta, new Rectangle(20, 0, Width - 90, Height), c.AccentSoft,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, _time, UiMetrics.MonoTimer, new Rectangle(Width - 86, 0, 52, Height), c.TextPrimary,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        UiIcons.Draw(g, "stop_circle", new Rectangle(Width - 24, 6, 16, 16), c.TextMuted);
    }
}
