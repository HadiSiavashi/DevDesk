using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class PageHeader : Panel
{
    private readonly Label _title = new() { AutoSize = false, Dock = DockStyle.Top, Height = 28 };
    private readonly Label _subtitle = new() { AutoSize = false, Dock = DockStyle.Top, Height = 20 };
    private readonly Panel _text = new() { Dock = DockStyle.Fill, Tag = "no-theme" };
    private readonly FlowLayoutPanel _actions = new()
    {
        Dock = DockStyle.Right,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(8, 4, 0, 0),
        Tag = "no-theme"
    };

    public PageHeader()
    {
        Height = 56;
        Dock = DockStyle.Top;
        Tag = "no-theme";
        Padding = new Padding(0, 0, 0, 8);
        _text.Controls.Add(_subtitle);
        _text.Controls.Add(_title);
        Controls.Add(_text);
        Controls.Add(_actions);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public string TitleText { get => _title.Text; set => _title.Text = value; }
    public string SubtitleText
    {
        get => _subtitle.Text;
        set
        {
            _subtitle.Text = value;
            _subtitle.Visible = !string.IsNullOrEmpty(value);
            Height = string.IsNullOrEmpty(value) ? 44 : 64;
        }
    }

    public FlowLayoutPanel Actions => _actions;

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.Background;
        _text.BackColor = c.Background;
        _title.Font = UiMetrics.PageTitle;
        _title.ForeColor = c.TextPrimary;
        _title.BackColor = c.Background;
        _subtitle.Font = UiMetrics.Body;
        _subtitle.ForeColor = c.TextSecondary;
        _subtitle.BackColor = c.Background;
        _actions.BackColor = c.Background;
    }
}
