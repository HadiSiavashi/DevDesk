using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class SearchBox : UserControl
{
    private readonly TextBox _input = new() { BorderStyle = BorderStyle.None };
    private bool _focused;
    public event EventHandler<string>? TextChangedDebounced;
    public event EventHandler? Activated;
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 200 };
    private string _hint = "Ctrl+K";

    public SearchBox()
    {
        Height = UiMetrics.ControlHeightCompact;
        MinimumSize = new Size(180, UiMetrics.ControlHeightCompact);
        DrawingUtil.EnableDoubleBuffer(this);
        _input.PlaceholderText = LocalizationService.Instance.Get("search.placeholder");
        _input.GotFocus += (_, _) => { _focused = true; Invalidate(); };
        _input.LostFocus += (_, _) => { _focused = false; Invalidate(); };
        _input.TextChanged += (_, _) => { _debounce.Stop(); _debounce.Start(); };
        _input.Click += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        Click += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            TextChangedDebounced?.Invoke(this, _input.Text);
        };
        Controls.Add(_input);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        Resize += (_, _) => LayoutInput();
        ApplyTheme();
        LayoutInput();
    }

    public TextBox Inner => _input;
    public string Query { get => _input.Text; set => _input.Text = value; }
    public string Hint { get => _hint; set { _hint = value; Invalidate(); } }
    public string Placeholder { get => _input.PlaceholderText; set => _input.PlaceholderText = value; }

    private void LayoutInput()
    {
        _input.Location = new Point(28, 5);
        _input.Size = new Size(Math.Max(40, Width - 80), Height - 10);
    }

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
        using (var pen = new Pen(_focused ? c.Accent : c.Border))
            DrawingUtil.DrawRounded(g, pen, rect, UiMetrics.RadiusSm);
        UiIcons.Draw(g, "search", new Rectangle(8, (Height - 16) / 2, 16, 16), c.TextMuted);

        var chips = _hint.Split('+');
        var x = Width - 8;
        for (var i = chips.Length - 1; i >= 0; i--)
        {
            var t = chips[i].Trim();
            var w = TextRenderer.MeasureText(t, UiMetrics.Kbd).Width + 6;
            x -= w;
            var kr = new Rectangle(x, (Height - 16) / 2, w, 16);
            using var kb = new SolidBrush(c.KbdBg);
            DrawingUtil.FillRounded(g, kb, kr, 3);
            using var kp = new Pen(c.Border);
            DrawingUtil.DrawRounded(g, kp, kr, 3);
            TextRenderer.DrawText(g, t, UiMetrics.Kbd, kr, c.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            x -= 4;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _debounce.Dispose();
        base.Dispose(disposing);
    }
}
