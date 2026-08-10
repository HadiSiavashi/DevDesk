using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class ErrorPanel : Panel
{
    private readonly Label _message = new() { Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter };
    private readonly ModernButton _retry = new() { Text = LocalizationService.Instance.Get("common.retry"), Dock = DockStyle.Bottom, Height = 40, Width = 120 };
    private Func<Task>? _retryAction;

    public ErrorPanel()
    {
        Dock = DockStyle.Fill;
        _retry.Click += async (_, _) =>
        {
            if (_retryAction is not null) await _retryAction();
        };
        Controls.Add(_retry);
        Controls.Add(_message);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public void SetError(Exception ex, Func<Task>? retry)
    {
        _message.Text = ex.Message;
        _retryAction = retry;
        _retry.Visible = retry is not null;
    }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.Background;
        _message.ForeColor = c.Error;
        _retry.Text = LocalizationService.Instance.Get("common.retry");
    }
}
