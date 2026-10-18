using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

/// <summary>Dense inventory row used by notes, snippets, bookmarks, and similar lists.</summary>
public sealed class InventoryRow : CardPanel
{
    private readonly Label _title = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _meta = new() { AutoSize = false, Dock = DockStyle.Right, Width = 180, TextAlign = ContentAlignment.MiddleRight };
    private bool _selected;
    private bool _hover;

    public InventoryRow()
    {
        Height = UiMetrics.TaskRowHeight;
        Padding = new Padding(UiMetrics.Space12, UiMetrics.Space4, UiMetrics.Space12, UiMetrics.Space4);
        Cursor = Cursors.Hand;
        Controls.Add(_title);
        Controls.Add(_meta);

        _title.Click += (_, _) => OnClick(EventArgs.Empty);
        _meta.Click += (_, _) => OnClick(EventArgs.Empty);
        DoubleClick += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        _title.DoubleClick += (_, _) => Activated?.Invoke(this, EventArgs.Empty);

        MouseEnter += (_, _) => { _hover = true; ApplyRowTheme(); };
        MouseLeave += (_, _) => { _hover = false; ApplyRowTheme(); };
        _title.MouseEnter += (_, _) => { _hover = true; ApplyRowTheme(); };
        _title.MouseLeave += (_, _) => { _hover = false; ApplyRowTheme(); };
        _meta.MouseEnter += (_, _) => { _hover = true; ApplyRowTheme(); };
        _meta.MouseLeave += (_, _) => { _hover = false; ApplyRowTheme(); };

        ApplyRowTheme();
        ThemeManager.Instance.Attach(this, (_, _) => ApplyRowTheme());
        LocalizationService.Instance.LanguageChanged += (_, _) => ApplyRowTheme();
        UiScale.Attach(this, (_, _) =>
        {
            Height = UiMetrics.TaskRowHeight;
            Padding = new Padding(UiMetrics.Space12, UiMetrics.Space4, UiMetrics.Space12, UiMetrics.Space4);
            ApplyRowTheme();
        });
    }

    public object? Item { get; set; }
    public event EventHandler? Activated;

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            AccentLeft = value;
            AccentColor = ThemeManager.Instance.Current.Accent;
            ApplyRowTheme();
        }
    }

    public void Bind(string title, string? meta = null)
    {
        _title.Text = title;
        _meta.Text = meta ?? "";
        _meta.Visible = !string.IsNullOrEmpty(meta);
        ApplyRowTheme();
    }

    private void ApplyRowTheme()
    {
        var c = ThemeManager.Instance.Current;
        var isRtl = LocalizationService.Instance.IsRtl;

        var bg = _selected ? c.SelectedBg : _hover ? c.HoverBg : c.Surface;
        BackColor = bg;

        _meta.Dock = isRtl ? DockStyle.Left : DockStyle.Right;
        _title.Dock = DockStyle.Fill;

        _title.Font = UiMetrics.Body;
        _title.ForeColor = c.TextPrimary;
        _title.BackColor = bg;
        _title.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;

        _meta.Font = UiMetrics.Meta;
        _meta.ForeColor = c.TextMuted;
        _meta.BackColor = bg;
        _meta.TextAlign = isRtl ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;

        Invalidate();
    }
}
