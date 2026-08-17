using DevDesk.Application.Dtos;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class AppTopBar : Panel
{
    public event EventHandler? SearchRequested;
    public event EventHandler? QuickAddRequested;
    public event EventHandler? FocusRequested;
    public event EventHandler? CollapseRequested;
    public event EventHandler? StopFocusRequested;

    private readonly IconButton _collapse = new();
    private readonly SearchBox _search = new();
    private readonly FocusChip _chip = new() { Visible = false };
    private readonly IconButton _timer = new() { Icon = "timer" };
    private readonly IconButton _bell = new() { Icon = "notifications" };
    private readonly ModernButton _newTask = new()
    {
        Text = "New Task",
        Icon = "add",
        AutoFit = true
    };

    public AppTopBar()
    {
        Tag = "no-theme";
        Dock = DockStyle.Top;
        Height = UiMetrics.TopBarHeight;
        DrawingUtil.EnableDoubleBuffer(this);
        _collapse.Click += (_, _) => CollapseRequested?.Invoke(this, EventArgs.Empty);
        _search.Activated += (_, _) => SearchRequested?.Invoke(this, EventArgs.Empty);
        _search.Click += (_, _) => SearchRequested?.Invoke(this, EventArgs.Empty);
        _chip.Click += (_, _) => FocusRequested?.Invoke(this, EventArgs.Empty);
        _chip.StopClicked += (_, _) => StopFocusRequested?.Invoke(this, EventArgs.Empty);
        _timer.Click += (_, _) => FocusRequested?.Invoke(this, EventArgs.Empty);
        _bell.Click += (_, _) => SearchRequested?.Invoke(this, EventArgs.Empty);
        _newTask.Click += (_, _) => QuickAddRequested?.Invoke(this, EventArgs.Empty);
        _search.Placeholder = LocalizationService.Instance.Get("search.placeholder");
        _search.Hint = "Ctrl+K";

        _collapse.Icon = "menu";
        Controls.Add(_collapse);
        Controls.Add(_search);
        Controls.Add(_chip);
        Controls.Add(_timer);
        Controls.Add(_bell);
        Controls.Add(_newTask);
        Resize += (_, _) => LayoutBar();
        ThemeManager.Instance.Attach(this, (_, _) => ApplyTheme());
        UiScale.Attach(this, (_, _) => ApplyScale());
        ApplyScale();
        ApplyTheme();
        LayoutBar();
    }

    public void SetFocusSession(FocusSessionDto? session)
    {
        _chip.Bind(session);
        LayoutBar();
    }

    public void SetNotificationBadge(bool on)
    {
        _bell.ShowBadge = on;
        _bell.Invalidate();
    }

    public void ApplyScale()
    {
        Height = UiMetrics.TopBarHeight;
        var ctrl = UiMetrics.ControlHeightCompact;
        _collapse.Size = new Size(ctrl, ctrl);
        _timer.Size = new Size(ctrl, ctrl);
        _bell.Size = new Size(ctrl, ctrl);
        _search.Height = ctrl;
        _search.MinimumSize = new Size(UiScale.Px(220), ctrl);
        _search.Width = Math.Clamp(Width / 3, UiScale.Px(220), UiScale.Px(520));
        _chip.Height = ctrl;
        _newTask.AutoFit = true;
        _newTask.Font = UiMetrics.Body;
        _newTask.FitToContents();
        LayoutBar();
        Invalidate();
    }

    public void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.TopBarBg;
        _newTask.Text = LocalizationService.Instance.Get("tasks.new") is var t && !t.StartsWith("tasks.") ? t : "New Task";
        _newTask.FitToContents();
        Invalidate();
        LayoutBar();
    }

    private void LayoutBar()
    {
        var ctrl = Math.Max(1, _collapse.Height);
        var y = (Height - ctrl) / 2;
        _collapse.Location = new Point(UiMetrics.Space8, y);
        _search.Location = new Point(UiScale.Px(44), y);
        _search.Width = Math.Clamp(Width / 3, UiScale.Px(220), UiScale.Px(520));

        _newTask.Location = new Point(Width - _newTask.Width - UiMetrics.Space12, y);
        _bell.Location = new Point(_newTask.Left - UiScale.Px(40), y);
        _timer.Location = new Point(_bell.Left - UiScale.Px(36), y);

        if (_chip.Visible)
        {
            _chip.Width = Math.Min(UiScale.Px(240), Math.Max(UiScale.Px(160), _timer.Left - _search.Right - UiMetrics.Space24));
            _chip.Location = new Point(_search.Right + UiMetrics.Space12, y);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        e.Graphics.Clear(c.TopBarBg);
        using var pen = new Pen(c.Border);
        e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        using var div = new Pen(c.OutlineVariant);
        e.Graphics.DrawLine(div, _bell.Right + 4, 14, _bell.Right + 4, Height - 14);
    }
}
