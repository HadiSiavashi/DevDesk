using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Dialogs;

/// <summary>Compact themed modal base with Escape-to-close and subtle open animation.</summary>
public class ModalForm : Form
{
    private float _opacityTarget = 1f;

    public ModalForm()
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        KeyPreview = true;
        Font = UiMetrics.Body;
        Opacity = 0;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape && !IsBusy)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
        Shown += (_, _) => AnimateIn();
        ThemeManager.Instance.ApplyTo(this);
    }

    protected bool IsBusy { get; set; }

    protected void AnimateIn()
    {
        Opacity = 0;
        AnimationScheduler.Instance.Animate(UiMetrics.ModalMs, t =>
        {
            if (!IsDisposed)
                Opacity = t * _opacityTarget;
        });
    }

    protected async Task AnimateOutAndCloseAsync(DialogResult result)
    {
        var tcs = new TaskCompletionSource();
        AnimationScheduler.Instance.Animate(UiMetrics.MicroMs, t =>
        {
            if (!IsDisposed)
                Opacity = 1f - t;
        }, () =>
        {
            if (!IsDisposed)
            {
                DialogResult = result;
                Close();
            }
            tcs.TrySetResult();
        });
        await tcs.Task;
    }

    protected string T(string key) => LocalizationService.Instance.Get(key);
}
