using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public abstract class ViewBase : UserControl
{
    protected readonly IServiceScopeFactory ScopeFactory;
    protected readonly NavigationService Navigation;
    protected readonly Panel ContentPanel;
    protected readonly LoadingPanel LoadingPanel;
    protected readonly ErrorPanel ErrorPanel;
    protected readonly EmptyStatePanel EmptyPanel;

    private EventHandler? _themeHandler;
    private EventHandler? _languageHandler;
    private bool _disposed;

    protected ViewBase(IServiceScopeFactory scopeFactory, NavigationService navigation)
    {
        ScopeFactory = scopeFactory;
        Navigation = navigation;
        Dock = DockStyle.Fill;

        ContentPanel = new Panel { Dock = DockStyle.Fill, Tag = "no-theme", Padding = new Padding(UiMetrics.Space16), AutoScroll = true };
        LoadingPanel = new LoadingPanel { Visible = false };
        ErrorPanel = new ErrorPanel { Visible = false };
        EmptyPanel = new EmptyStatePanel { Visible = false };

        Controls.Add(ContentPanel);
        Controls.Add(LoadingPanel);
        Controls.Add(ErrorPanel);
        Controls.Add(EmptyPanel);

        _themeHandler = (_, _) => { if (!_disposed && !IsDisposed) OnThemeChanged(); };
        _languageHandler = (_, _) => { if (!_disposed && !IsDisposed) OnLanguageChanged(); };
        ThemeManager.Instance.ThemeChanged += _themeHandler;
        LocalizationService.Instance.LanguageChanged += _languageHandler;
        Load += OnViewLoad;

        OnThemeChanged();
    }

    protected virtual void OnThemeChanged()
    {
        if (IsDisposed) return;
        BackColor = ThemeManager.Instance.Current.Background;
        ThemeManager.Instance.ApplyTo(this);
    }

    protected virtual void OnLanguageChanged()
    {
        if (IsDisposed) return;
        LocalizationService.Instance.ApplyRtl(this);
    }

    private async void OnViewLoad(object? sender, EventArgs e)
    {
        if (_disposed || IsDisposed) return;
        await LoadAsync();
    }

    protected abstract Task LoadAsync();

    protected void ShowLoading()
    {
        if (IsDisposed) return;
        ContentPanel.Visible = false;
        ErrorPanel.Visible = false;
        EmptyPanel.Visible = false;
        LoadingPanel.Visible = true;
        LoadingPanel.BringToFront();
    }

    protected void ShowContent()
    {
        if (IsDisposed) return;
        LoadingPanel.Visible = false;
        ErrorPanel.Visible = false;
        ContentPanel.Visible = true;
        ContentPanel.BringToFront();
        ThemeManager.Instance.ApplyTo(this);
        DrawingUtil.ApplyWindowChrome(ContentPanel);
    }

    protected void ShowEmpty(string? message = null)
    {
        if (IsDisposed) return;
        LoadingPanel.Visible = false;
        ErrorPanel.Visible = false;
        ContentPanel.Visible = false;
        if (message is not null) EmptyPanel.Message = message;
        EmptyPanel.Visible = true;
        EmptyPanel.BringToFront();
    }

    protected void ShowError(Exception ex, Func<Task>? retry = null)
    {
        if (IsDisposed) return;
        LoadingPanel.Visible = false;
        ContentPanel.Visible = false;
        EmptyPanel.Visible = false;
        ErrorPanel.SetError(ex, retry ?? LoadAsync);
        ErrorPanel.Visible = true;
        ErrorPanel.BringToFront();
    }

    protected string T(string key) => LocalizationService.Instance.Get(key);

    protected TService GetService<TService>(IServiceScope scope) where TService : notnull
        => scope.ServiceProvider.GetRequiredService<TService>();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            if (_themeHandler is not null)
                ThemeManager.Instance.ThemeChanged -= _themeHandler;
            if (_languageHandler is not null)
                LocalizationService.Instance.LanguageChanged -= _languageHandler;
        }
        base.Dispose(disposing);
    }
}
