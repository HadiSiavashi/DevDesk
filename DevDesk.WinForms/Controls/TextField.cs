using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class TextField : UserControl
{
    private readonly TextBox _input = new() { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill };
    private bool _focused;

    public TextField()
    {
        Height = UiMetrics.InputHeight;
        Padding = new Padding(10, 7, 10, 7);
        DrawingUtil.EnableDoubleBuffer(this);
        Controls.Add(_input);
        _input.GotFocus += (_, _) => { _focused = true; Invalidate(); };
        _input.LostFocus += (_, _) => { _focused = false; Invalidate(); };
        _input.TextChanged += (_, _) => TextChanged?.Invoke(this, EventArgs.Empty);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public new string Text { get => _input.Text; set => _input.Text = value ?? ""; }
    public string PlaceholderText { get => _input.PlaceholderText; set => _input.PlaceholderText = value; }
    public bool ReadOnly { get => _input.ReadOnly; set => _input.ReadOnly = value; }
    public bool Multiline
    {
        get => _input.Multiline;
        set
        {
            _input.Multiline = value;
            _input.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None;
        }
    }
    public new event EventHandler? TextChanged;
    public TextBox Inner => _input;

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.InputBg;
        _input.BackColor = c.InputBg;
        _input.ForeColor = c.TextPrimary;
        _input.Font = UiMetrics.Body;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var bg = new SolidBrush(c.InputBg))
            DrawingUtil.FillRounded(g, bg, rect, UiMetrics.RadiusSm);
        using var pen = new Pen(_focused ? c.Accent : c.Border, _focused ? 1.5f : 1f);
        DrawingUtil.DrawRounded(g, pen, rect, UiMetrics.RadiusSm);
    }
}
