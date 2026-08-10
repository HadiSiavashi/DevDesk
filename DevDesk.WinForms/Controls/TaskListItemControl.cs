using DevDesk.Application.Dtos;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class TaskListItemControl : Panel
{
    public event EventHandler<Guid>? ItemClicked;
    public event EventHandler<Guid>? CompleteClicked;

    private readonly Panel _accent = new() { Width = 3, Dock = DockStyle.Left };
    private readonly CheckBox _check = new() { Width = 18, Height = 18 };
    private readonly Label _title = new() { AutoEllipsis = true, Font = UiMetrics.TaskTitle };
    private readonly Label _meta = new() { AutoEllipsis = true, Font = UiMetrics.Meta };
    private Guid _id;
    private bool _suppressCheckedChanged;
    private bool _selected;
    private bool _hover;
    private EventHandler? _themeHandler;
    private Color _flashColor = Color.Empty;

    public Guid TaskId => _id;

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            ApplyTheme();
        }
    }

    public TaskListItemControl()
    {
        Height = UiMetrics.TaskRowHeight;
        MinimumSize = new Size(200, UiMetrics.TaskRowHeight);
        Cursor = Cursors.Hand;
        Padding = new Padding(0);
        DoubleBuffered = true;

        _check.Location = new Point(12, 17);
        _title.Location = new Point(36, 8);
        _title.Height = 20;
        _meta.Location = new Point(36, 28);
        _meta.Height = 16;

        Controls.Add(_title);
        Controls.Add(_meta);
        Controls.Add(_check);
        Controls.Add(_accent);

        Click += OnItemClick;
        _title.Click += OnItemClick;
        _meta.Click += OnItemClick;
        _check.CheckedChanged += OnCheckChanged;
        MouseEnter += (_, _) => { _hover = true; ApplyTheme(); };
        MouseLeave += (_, _) => { _hover = false; ApplyTheme(); };

        Resize += (_, _) => LayoutContent();
        _themeHandler = (_, _) => ApplyTheme();
        ThemeManager.Instance.ThemeChanged += _themeHandler;
        ApplyTheme();
    }

    public void Bind(TaskListItemDto item)
    {
        _id = item.Id;
        _title.Text = item.Title;
        _meta.Text = FormatMeta(item);
        _accent.BackColor = PriorityColor(item.Priority);

        _suppressCheckedChanged = true;
        try
        {
            _check.Checked = item.Status == WorkTaskStatus.Done;
        }
        finally
        {
            _suppressCheckedChanged = false;
        }

        if (item.Status == WorkTaskStatus.Done)
        {
            _title.Font = new Font(UiMetrics.TaskTitle, FontStyle.Strikeout);
        }
        else
        {
            _title.Font = UiMetrics.TaskTitle;
        }

        ApplyTheme();
        LayoutContent();
    }

    public void FlashHighlight()
    {
        var accent = ThemeManager.Instance.Current.Accent;
        _flashColor = Color.FromArgb(60, accent);
        ApplyTheme();
        AnimationScheduler.Instance.Animate(UiMetrics.ListMs * 2, t =>
        {
            if (IsDisposed) return;
            var a = (int)(60 * (1f - t));
            _flashColor = Color.FromArgb(a, accent);
            ApplyTheme();
        }, () =>
        {
            if (IsDisposed) return;
            _flashColor = Color.Empty;
            ApplyTheme();
        });
    }

    private static string FormatMeta(TaskListItemDto item)
    {
        var parts = new List<string>();
        parts.Add(item.ProjectName ?? "Inbox");
        parts.Add(item.Priority.ToString());
        if (item.IsOverdue)
            parts.Add("Overdue");
        else if (item.DueDate.HasValue)
            parts.Add(item.DueDate.Value.ToString("MMM d"));
        if (item.EstimatedMinutes.HasValue)
            parts.Add($"{item.EstimatedMinutes}m");
        return string.Join(" · ", parts);
    }

    private static Color PriorityColor(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => ThemeManager.Instance.Current.Success,
        TaskPriority.Medium => ThemeManager.Instance.Current.Warning,
        TaskPriority.High => ThemeManager.Instance.Current.Error,
        TaskPriority.Critical => ThemeManager.Instance.Current.Error,
        _ => ThemeManager.Instance.Current.Border
    };

    private void OnItemClick(object? sender, EventArgs e) => ItemClicked?.Invoke(this, _id);

    private void OnCheckChanged(object? sender, EventArgs e)
    {
        if (_suppressCheckedChanged) return;
        if (_check.Checked)
            CompleteClicked?.Invoke(this, _id);
        else
            _check.Checked = true;
    }

    private void LayoutContent()
    {
        var w = Math.Max(100, ClientSize.Width - 48);
        _title.Width = w;
        _meta.Width = w;
    }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        if (_flashColor.A > 0)
            BackColor = _flashColor;
        else if (_selected)
            BackColor = c.SelectedBg;
        else if (_hover)
            BackColor = c.HoverBg;
        else
            BackColor = c.Surface;

        _title.ForeColor = _check.Checked ? c.TextMuted : c.TextPrimary;
        _meta.ForeColor = c.TextMuted;
        _title.BackColor = BackColor;
        _meta.BackColor = BackColor;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _themeHandler is not null)
            ThemeManager.Instance.ThemeChanged -= _themeHandler;
        base.Dispose(disposing);
    }
}
