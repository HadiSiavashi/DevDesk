using DevDesk.Application.Events;
using DevDesk.WinForms.Controls;

namespace DevDesk.WinForms.Services;

/// <summary>UI-facing helpers for toast + marshalling app events onto the UI thread.</summary>
public sealed class UiNotifier
{
    private readonly ToastHost _toast;
    private readonly Control _marshal;
    private readonly IAppEventBus _events;
    private EventHandler<AppEvent>? _handler;

    public UiNotifier(ToastHost toast, Control marshal, IAppEventBus events)
    {
        _toast = toast;
        _marshal = marshal;
        _events = events;
    }

    public void Attach()
    {
        _handler = OnEvent;
        _events.Published += _handler;
    }

    public void Detach()
    {
        if (_handler is not null)
            _events.Published -= _handler;
    }

    public void Toast(string message, bool isError = false) => _toast.ShowToast(message, isError);

    private void OnEvent(object? sender, AppEvent e)
    {
        if (_marshal.IsDisposed) return;
        if (_marshal.InvokeRequired)
        {
            _marshal.BeginInvoke(() => OnEvent(sender, e));
            return;
        }

        switch (e.Kind)
        {
            case AppEventKind.TaskCreated:
                _toast.ShowToast("Task created");
                break;
            case AppEventKind.TaskUpdated:
                _toast.ShowToast("Task updated");
                break;
            case AppEventKind.TaskCompleted:
                _toast.ShowToast("Task completed");
                break;
            case AppEventKind.TaskDeleted:
                _toast.ShowToast("Task deleted");
                break;
            case AppEventKind.FocusStarted:
                _toast.ShowToast("Focus started");
                break;
            case AppEventKind.FocusPaused:
                _toast.ShowToast("Focus paused");
                break;
            case AppEventKind.FocusStopped:
                _toast.ShowToast("Focus stopped");
                break;
        }
    }
}
