using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

/// <summary>Dense inventory row used by notes, snippets, bookmarks, and similar lists.</summary>
public sealed class InventoryRow : CardPanel
{
    private readonly Label _title = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _meta = new() { AutoSize = false, Dock = DockStyle.Right, Width = 180, TextAlign = ContentAlignment.MiddleRight };

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
        ApplyRowTheme();
        ThemeManager.Instance.Attach(this, (_, _) => ApplyRowTheme());
        UiScale.Attach(this, (_, _) =>
        {
            Height = UiMetrics.TaskRowHeight;
            Padding = new Padding(UiMetrics.Space12, UiMetrics.Space4, UiMetrics.Space12, UiMetrics.Space4);
            ApplyRowTheme();
        });
    }

    public object? Item { get; set; }
    public event EventHandler? Activated;

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
        _title.Font = UiMetrics.Body;
        _title.ForeColor = c.TextPrimary;
        _title.BackColor = c.Surface;
        _meta.Font = UiMetrics.Meta;
        _meta.ForeColor = c.TextMuted;
        _meta.BackColor = c.Surface;
    }
}
