using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class SearchBox : UserControl
{
    private readonly TextBox _input = new() { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill };

    public event EventHandler<string>? TextChangedDebounced;

    public SearchBox()
    {
        Height = 36;
        Padding = new Padding(8, 8, 8, 8);
        _input.PlaceholderText = LocalizationService.Instance.Get("common.search");
        _input.TextChanged += (_, _) => TextChangedDebounced?.Invoke(this, _input.Text);
        Controls.Add(_input);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public string Query { get => _input.Text; set => _input.Text = value; }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.InputBg;
        _input.BackColor = c.InputBg;
        _input.ForeColor = c.TextPrimary;
    }
}
