using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class ToastHost : Panel
{
    private readonly System.Windows.Forms.Timer _hideTimer = new();
    private Label? _label;

    public ToastHost()
    {
        Dock = DockStyle.Bottom;
        Height = 0;
        Tag = "no-theme";
        _hideTimer.Tick += (_, _) => BeginHide();
    }

    public void ShowToast(string message, bool isError = false)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ShowToast(message, isError));
            return;
        }

        Controls.Clear();
        var c = ThemeManager.Instance.Current;
        _label = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = isError ? c.Error : c.Accent,
            ForeColor = Color.White,
            Font = UiMetrics.Body,
            Padding = new Padding(UiMetrics.Space8)
        };
        Controls.Add(_label);
        Height = 0;
        AnimationScheduler.Instance.Animate(UiMetrics.MicroMs, t =>
        {
            if (!IsDisposed)
                Height = (int)(UiMetrics.ToastHeight * t);
        });
        _hideTimer.Stop();
        _hideTimer.Interval = UiMetrics.ToastMs;
        _hideTimer.Start();
    }

    private void BeginHide()
    {
        _hideTimer.Stop();
        var start = Height;
        AnimationScheduler.Instance.Animate(UiMetrics.MicroMs, t =>
        {
            if (!IsDisposed)
                Height = (int)(start * (1f - t));
        }, () =>
        {
            if (!IsDisposed)
            {
                Controls.Clear();
                Height = 0;
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hideTimer.Dispose();
        base.Dispose(disposing);
    }
}
