using DevDesk.Application.Dtos;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class TaskListItemControl : Panel
{
    public event EventHandler<Guid>? ItemClicked;
    public event EventHandler<Guid>? CompleteClicked;
    public event EventHandler<Guid>? EditClicked;
    public event EventHandler<Guid>? StarClicked;

    private Guid _id;
    private bool _done;
    private bool _selected;
    private bool _hover;
    private bool _starred;
    private TaskPriority _priority = TaskPriority.Medium;
    private string _title = "";
    private string _project = "";
    private string _due = "";
    private string _checklist = "";
    private bool _overdue;
    private Color _flashColor = Color.Empty;
    private EventHandler? _themeHandler;

    public Guid TaskId => _id;
    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public TaskListItemControl()
    {
        Height = UiMetrics.TaskRowHeight;
        MinimumSize = new Size(200, UiMetrics.TaskRowHeight);
        Cursor = Cursors.Hand;
        Tag = "no-theme";
        DrawingUtil.EnableDoubleBuffer(this);
        _themeHandler = (_, _) => Invalidate();
        ThemeManager.Instance.ThemeChanged += _themeHandler;
        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        MouseClick += OnClick;
    }

    public void Bind(TaskListItemDto item)
    {
        _id = item.Id;
        _title = item.Title;
        _project = item.ProjectName ?? "Inbox";
        _done = item.Status == WorkTaskStatus.Done;
        _starred = item.IsStarred;
        _priority = item.Priority;
        _overdue = item.IsOverdue;
        _due = item.IsOverdue ? "Overdue" : item.DueDate?.ToString("MMM d") ?? "";
        _checklist = item.ChecklistTotal > 0 ? $"{item.ChecklistCompleted}/{item.ChecklistTotal}" : "";
        Invalidate();
    }

    public void FlashHighlight()
    {
        var accent = ThemeManager.Instance.Current.Accent;
        AnimationScheduler.Instance.Animate(UiMetrics.ListMs * 2, t =>
        {
            if (IsDisposed) return;
            _flashColor = Color.FromArgb((int)(60 * (1f - t)), accent);
            Invalidate();
        }, () =>
        {
            if (IsDisposed) return;
            _flashColor = Color.Empty;
            Invalidate();
        });
    }

    private void OnClick(object? sender, MouseEventArgs e)
    {
        var check = new Rectangle(12, (Height - 18) / 2, 18, 18);
        if (check.Contains(e.Location))
        {
            if (!_done) CompleteClicked?.Invoke(this, _id);
            return;
        }
        if (_hover)
        {
            var edit = new Rectangle(Width - 52, (Height - 20) / 2, 20, 20);
            var more = new Rectangle(Width - 28, (Height - 20) / 2, 20, 20);
            if (edit.Contains(e.Location)) { EditClicked?.Invoke(this, _id); return; }
            if (more.Contains(e.Location)) { StarClicked?.Invoke(this, _id); return; }
        }
        ItemClicked?.Invoke(this, _id);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var bg = _flashColor.A > 0 ? _flashColor : _selected ? c.SelectedBg : _hover ? c.HoverBg : c.Surface;
        using (var brush = new SolidBrush(bg))
            DrawingUtil.FillRounded(g, brush, new Rectangle(0, 0, Width - 1, Height - 1), UiMetrics.RadiusSm);
        if (_selected)
        {
            using var pen = new Pen(c.Accent);
            DrawingUtil.DrawRounded(g, pen, new Rectangle(0, 0, Width - 1, Height - 1), UiMetrics.RadiusSm);
        }

        var check = new Rectangle(12, (Height - 16) / 2, 16, 16);
        using (var cp = new Pen(_hover ? c.Accent : c.OutlineVariant))
            DrawingUtil.DrawRounded(g, cp, check, 3);
        if (_done)
            UiIcons.Draw(g, "check", check, c.Accent, 1.6f);

        var titleColor = _done ? c.TextMuted : c.TextPrimary;
        var titleRect = new Rectangle(36, 6, Width - 140, 20);
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
        var font = _done ? new Font(UiMetrics.Body, FontStyle.Strikeout) : UiMetrics.Body;
        TextRenderer.DrawText(g, _title, font, titleRect, titleColor, flags);
        if (_done) font.Dispose();

        var metaX = 36;
        var metaY = 26;
        if (_priority is TaskPriority.High or TaskPriority.Critical)
        {
            var badge = "HIGH";
            var bw = TextRenderer.MeasureText(badge, UiMetrics.Kbd).Width + 6;
            var br = new Rectangle(metaX, metaY, bw, 14);
            using var bb = new SolidBrush(DrawingUtil.WithAlpha(c.Error, 60));
            DrawingUtil.FillRounded(g, bb, br, 3);
            TextRenderer.DrawText(g, badge, UiMetrics.Kbd, br, c.Error, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            metaX += bw + 8;
        }
        if (!string.IsNullOrEmpty(_due))
        {
            var dueColor = _overdue ? c.Error : c.AccentSoft;
            TextRenderer.DrawText(g, _due, UiMetrics.Meta, new Point(metaX, metaY), dueColor);
            metaX += TextRenderer.MeasureText(_due, UiMetrics.Meta).Width + 8;
        }
        TextRenderer.DrawText(g, _project, UiMetrics.Meta, new Point(metaX, metaY), c.TextMuted);
        if (!string.IsNullOrEmpty(_checklist))
        {
            var cw = TextRenderer.MeasureText(_checklist, UiMetrics.Meta).Width;
            TextRenderer.DrawText(g, _checklist, UiMetrics.Meta, new Point(Width - 90 - cw, metaY), c.TextMuted);
        }

        if (_hover || _starred)
        {
            UiIcons.Draw(g, "star", new Rectangle(Width - 28, (Height - 16) / 2, 16, 16), _starred ? c.Tertiary : c.TextMuted);
            if (_hover)
                UiIcons.Draw(g, "edit", new Rectangle(Width - 52, (Height - 16) / 2, 16, 16), c.TextMuted);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _themeHandler is not null)
            ThemeManager.Instance.ThemeChanged -= _themeHandler;
        base.Dispose(disposing);
    }
}
