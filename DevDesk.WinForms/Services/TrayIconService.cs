using DevDesk.WinForms.Localization;

namespace DevDesk.WinForms.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private Form? _mainForm;

    public Action? StartFocusRequested { get; set; }
    public Action? PauseFocusRequested { get; set; }
    public Action? StopFocusRequested { get; set; }
    public Action? QuickAddRequested { get; set; }

    public TrayIconService()
    {
        _menu = new ContextMenuStrip();
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = LocalizationService.Instance.Get("app.title"),
            Visible = false,
            ContextMenuStrip = _menu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMain();
        RebuildMenu();
    }

    public void Attach(Form mainForm)
    {
        _mainForm = mainForm;
    }

    public void SetVisible(bool visible) => _notifyIcon.Visible = visible;

    public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        if (!_notifyIcon.Visible) _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    public void RebuildMenu()
    {
        var loc = LocalizationService.Instance;
        _menu.Items.Clear();
        var show = new ToolStripMenuItem(loc.Get("tray.show"), null, (_, _) => ShowMain());
        var startFocus = new ToolStripMenuItem(loc.Get("tray.startFocus"), null, (_, _) => StartFocusRequested?.Invoke());
        var pauseFocus = new ToolStripMenuItem(loc.Get("tray.pauseFocus"), null, (_, _) => PauseFocusRequested?.Invoke());
        var stopFocus = new ToolStripMenuItem(loc.Get("tray.stopFocus"), null, (_, _) => StopFocusRequested?.Invoke());
        var quickAdd = new ToolStripMenuItem(loc.Get("tray.quickAdd"), null, (_, _) => QuickAddRequested?.Invoke());
        var exit = new ToolStripMenuItem(loc.Get("tray.exit"), null, (_, _) =>
        {
            _notifyIcon.Visible = false;
            System.Windows.Forms.Application.Exit();
        });
        _menu.Items.Add(show);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(startFocus);
        _menu.Items.Add(pauseFocus);
        _menu.Items.Add(stopFocus);
        _menu.Items.Add(quickAdd);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exit);
    }

    private void ShowMain()
    {
        if (_mainForm is null) return;
        _mainForm.Show();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.Activate();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
