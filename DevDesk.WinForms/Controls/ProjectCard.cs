using DevDesk.Application.Dtos;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class ProjectCard : CardPanel
{
    public event EventHandler<Guid>? OpenRequested;
    public event EventHandler<Guid>? DeleteRequested;

    private Guid _id;
    private readonly Label _name = new() { AutoSize = false, Height = 22 };
    private readonly Label _desc = new() { AutoSize = false, Height = 32 };
    private readonly Label _progress = new() { AutoSize = false, Height = 16 };
    private readonly ProgressBarControl _bar = new() { Height = 6, Dock = DockStyle.Bottom };
    private readonly IconButton _more = new() { Icon = "more_vert", Size = new Size(24, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right };
    private bool _hover;

    public ProjectCard()
    {
        Size = new Size(UiScale.Px(260), UiScale.Px(148));
        Margin = new Padding(0, 0, UiMetrics.Space16, UiMetrics.Space16);
        Padding = new Padding(UiMetrics.Space12);
        Cursor = Cursors.Hand;
        _more.Click += (_, _) =>
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (_, _) => OpenRequested?.Invoke(this, _id));
            menu.Items.Add("Delete", null, (_, _) => DeleteRequested?.Invoke(this, _id));
            menu.Show(_more, new Point(0, _more.Height));
        };
        Controls.Add(_bar);
        Controls.Add(_progress);
        Controls.Add(_desc);
        Controls.Add(_name);
        Controls.Add(_more);
        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        Click += (_, _) => OpenRequested?.Invoke(this, _id);
        _name.Click += (_, _) => OpenRequested?.Invoke(this, _id);
        _desc.Click += (_, _) => OpenRequested?.Invoke(this, _id);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyLocal();
        Resize += (_, _) => LayoutLocal();
        ApplyLocal();
    }

    public void Bind(ProjectListItemDto project)
    {
        _id = project.Id;
        _name.Text = project.Name;
        _desc.Text = $"{project.CompletedTasks}/{project.TotalTasks} tasks";
        var pct = project.ProgressPercent;
        _progress.Text = $"Progress    {pct:F0}%";
        _bar.Value = (float)Math.Clamp(pct / 100.0, 0, 1);
        ApplyLocal();
        LayoutLocal();
    }

    private void LayoutLocal()
    {
        var pad = UiMetrics.Space12;
        var more = UiMetrics.IconButtonSize;
        _more.Size = new Size(more, more);
        _more.Location = new Point(Width - more - pad, pad);
        _name.Height = UiMetrics.LineTitle;
        _desc.Height = UiMetrics.LineBody;
        _progress.Height = UiMetrics.LineMeta;
        _bar.Height = UiMetrics.ProgressHeight;
        _name.SetBounds(pad, pad + UiMetrics.Space4, Width - more - pad * 2, _name.Height);
        _desc.SetBounds(pad, _name.Bottom + UiMetrics.Space4, Width - pad * 2, _desc.Height);
        _bar.SetBounds(pad, Height - pad - _bar.Height, Width - pad * 2, _bar.Height);
        _progress.SetBounds(pad, _bar.Top - _progress.Height - UiMetrics.Space4, Width - pad * 2, _progress.Height);
    }

    private void ApplyLocal()
    {
        var c = ThemeManager.Instance.Current;
        _name.Font = UiMetrics.SectionTitle;
        _name.ForeColor = c.TextPrimary;
        _name.BackColor = c.Surface;
        _desc.Font = UiMetrics.Meta;
        _desc.ForeColor = c.TextMuted;
        _desc.BackColor = c.Surface;
        _progress.Font = UiMetrics.Meta;
        _progress.ForeColor = c.TextMuted;
        _progress.BackColor = c.Surface;
        AccentColor = c.Accent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var c = ThemeManager.Instance.Current;
        using var accent = new SolidBrush(c.Accent);
        e.Graphics.FillRectangle(accent, 1, 1, Width - 3, 4);
        if (_hover)
        {
            using var overlay = new SolidBrush(DrawingUtil.WithAlpha(c.Overlay, 180));
            e.Graphics.FillRectangle(overlay, ClientRectangle);
            TextRenderer.DrawText(e.Graphics, "Open Project", UiMetrics.SectionTitle, ClientRectangle, c.TextPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
