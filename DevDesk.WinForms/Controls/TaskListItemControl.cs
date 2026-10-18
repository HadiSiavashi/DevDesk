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
        UiScale.Attach(this, (_, _) =>
        {
            Height = UiMetrics.TaskRowHeight;
            MinimumSize = new Size(200, UiMetrics.TaskRowHeight);
            Invalidate();
        });
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

    private Rectangle GetCheckRect()
    {
        var isRtl = Localization.LocalizationService.Instance.IsRtl;
        var size = UiScale.Px(18);
        return isRtl
            ? new Rectangle(Width - UiScale.Px(14) - size, (Height - size) / 2, size, size)
            : new Rectangle(UiScale.Px(14), (Height - size) / 2, size, size);
    }

    private Rectangle GetEditRect()
    {
        var isRtl = Localization.LocalizationService.Instance.IsRtl;
        var size = UiScale.Px(18);
        return isRtl
            ? new Rectangle(UiScale.Px(36), (Height - size) / 2, size, size)
            : new Rectangle(Width - UiScale.Px(52), (Height - size) / 2, size, size);
    }

    private Rectangle GetStarRect()
    {
        var isRtl = Localization.LocalizationService.Instance.IsRtl;
        var size = UiScale.Px(18);
        return isRtl
            ? new Rectangle(UiScale.Px(12), (Height - size) / 2, size, size)
            : new Rectangle(Width - UiScale.Px(28), (Height - size) / 2, size, size);
    }

    private void OnClick(object? sender, MouseEventArgs e)
    {
        if (GetCheckRect().Contains(e.Location))
        {
            if (!_done) CompleteClicked?.Invoke(this, _id);
            return;
        }
        if (_hover || _starred)
        {
            if (GetStarRect().Contains(e.Location)) { StarClicked?.Invoke(this, _id); return; }
        }
        if (_hover)
        {
            if (GetEditRect().Contains(e.Location)) { EditClicked?.Invoke(this, _id); return; }
        }
        ItemClicked?.Invoke(this, _id);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        var isRtl = Localization.LocalizationService.Instance.IsRtl;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var bg = _flashColor.A > 0 ? _flashColor : _selected ? c.SelectedBg : _hover ? c.HoverBg : c.Surface;
        using (var brush = new SolidBrush(bg))
            DrawingUtil.FillRounded(g, brush, new Rectangle(0, 0, Width - 1, Height - 1), UiMetrics.RadiusSm);
        if (_selected)
        {
            using var pen = new Pen(c.Accent);
            DrawingUtil.DrawRounded(g, pen, new Rectangle(0, 0, Width - 1, Height - 1), UiMetrics.RadiusSm);
        }

        var check = GetCheckRect();
        using (var cp = new Pen(_hover ? c.Accent : c.OutlineVariant))
            DrawingUtil.DrawRounded(g, cp, check, 3);
        if (_done)
            UiIcons.Draw(g, "check", check, c.Accent, 1.6f);

        var titleColor = _done ? c.TextMuted : c.TextPrimary;
        var titleTop = UiScale.Px(8);
        var titleH = UiScale.Px(22);
        var titleRect = isRtl
            ? new Rectangle(UiScale.Px(64), titleTop, Width - UiScale.Px(110), titleH)
            : new Rectangle(UiScale.Px(40), titleTop, Width - UiScale.Px(110), titleH);

        var flags = (isRtl ? TextFormatFlags.Right : TextFormatFlags.Left)
                  | TextFormatFlags.EndEllipsis
                  | TextFormatFlags.Top;

        var font = _done ? new Font(UiMetrics.Body, FontStyle.Strikeout) : UiMetrics.Body;
        TextRenderer.DrawText(g, _title, font, titleRect, titleColor, flags);
        if (_done) font.Dispose();

        var metaY = UiScale.Px(32);
        var metaH = UiScale.Px(16);

        if (!isRtl)
        {
            var metaX = UiScale.Px(40);
            if (_priority is TaskPriority.High or TaskPriority.Critical)
            {
                var badge = _priority == TaskPriority.Critical ? "CRIT" : "HIGH";
                var bw = TextRenderer.MeasureText(badge, UiMetrics.Kbd).Width + UiScale.Px(8);
                var br = new Rectangle(metaX, metaY, bw, metaH);
                using var bb = new SolidBrush(DrawingUtil.WithAlpha(c.Error, 50));
                DrawingUtil.FillRounded(g, bb, br, 3);
                TextRenderer.DrawText(g, badge, UiMetrics.Kbd, br, c.Error, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                metaX += bw + UiScale.Px(8);
            }
            if (!string.IsNullOrEmpty(_due))
            {
                var dueColor = _overdue ? c.Error : c.AccentSoft;
                var dw = TextRenderer.MeasureText(_due, UiMetrics.Meta).Width;
                var dr = new Rectangle(metaX, metaY, dw, metaH);
                TextRenderer.DrawText(g, _due, UiMetrics.Meta, dr, dueColor, TextFormatFlags.VerticalCenter);
                metaX += dw + UiScale.Px(8);
            }
            var projectW = Math.Max(40, Width - metaX - UiScale.Px(80));
            var pr = new Rectangle(metaX, metaY, projectW, metaH);
            TextRenderer.DrawText(g, _project, UiMetrics.Meta, pr, c.TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (!string.IsNullOrEmpty(_checklist))
            {
                var cw = TextRenderer.MeasureText(_checklist, UiMetrics.Meta).Width;
                var cr = new Rectangle(Width - UiScale.Px(80) - cw, metaY, cw, metaH);
                TextRenderer.DrawText(g, _checklist, UiMetrics.Meta, cr, c.TextMuted, TextFormatFlags.VerticalCenter);
            }
        }
        else
        {
            var metaX = Width - UiScale.Px(42);
            if (_priority is TaskPriority.High or TaskPriority.Critical)
            {
                var badge = _priority == TaskPriority.Critical ? "بحرانی" : "بالا";
                var bw = TextRenderer.MeasureText(badge, UiMetrics.Kbd).Width + UiScale.Px(8);
                metaX -= bw;
                var br = new Rectangle(metaX, metaY, bw, metaH);
                using var bb = new SolidBrush(DrawingUtil.WithAlpha(c.Error, 50));
                DrawingUtil.FillRounded(g, bb, br, 3);
                TextRenderer.DrawText(g, badge, UiMetrics.Kbd, br, c.Error, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                metaX -= UiScale.Px(8);
            }
            if (!string.IsNullOrEmpty(_due))
            {
                var dueColor = _overdue ? c.Error : c.AccentSoft;
                var dw = TextRenderer.MeasureText(_due, UiMetrics.Meta).Width;
                metaX -= dw;
                var dr = new Rectangle(metaX, metaY, dw, metaH);
                TextRenderer.DrawText(g, _due, UiMetrics.Meta, dr, dueColor, TextFormatFlags.VerticalCenter);
                metaX -= UiScale.Px(8);
            }
            var pw = TextRenderer.MeasureText(_project, UiMetrics.Meta).Width;
            metaX -= pw;
            var pr = new Rectangle(metaX, metaY, pw, metaH);
            TextRenderer.DrawText(g, _project, UiMetrics.Meta, pr, c.TextMuted, TextFormatFlags.VerticalCenter);

            if (!string.IsNullOrEmpty(_checklist))
            {
                var cr = new Rectangle(UiScale.Px(64), metaY, UiScale.Px(60), metaH);
                TextRenderer.DrawText(g, _checklist, UiMetrics.Meta, cr, c.TextMuted, TextFormatFlags.VerticalCenter);
            }
        }

        if (_hover || _starred)
        {
            var starRect = GetStarRect();
            UiIcons.Draw(g, _starred ? "star" : "star_outline", starRect, _starred ? c.Warning : c.TextMuted);

            if (_hover)
            {
                var editRect = GetEditRect();
                UiIcons.Draw(g, "edit", editRect, c.TextMuted);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _themeHandler is not null)
            ThemeManager.Instance.ThemeChanged -= _themeHandler;
        base.Dispose(disposing);
    }
}
