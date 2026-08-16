using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Application.Options;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DevDesk.WinForms.Views;

public sealed class SettingsView : ViewBase
{
    private AppPreferencesDto _prefs = new();
    private string? _culture;
    private bool _minimizeToTray;
    private bool _alwaysOnTop;
    private bool _startMinimized;
    private bool _startWithWindows;

    public SettingsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        BuildTabs();
    }

    private void BuildTabs()
    {
        ContentPanel.Controls.Clear();
        var nav = new SettingsNavList { Dock = DockStyle.Left, Width = 220 };
        var host = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16, 8, 8, 8), Tag = "no-theme" };
        var builders = new Func<Panel>[]
        {
            () => Wrap(BuildGeneralTab()),
            () => Wrap(BuildAppearanceTab()),
            () => Wrap(BuildLanguageTab()),
            () => Wrap(BuildNotificationsTab()),
            () => Wrap(BuildFocusTab()),
            () => Wrap(BuildPomodoroTab()),
            () => Wrap(BuildShortcutsTab()),
            () => Wrap(BuildDatabaseTab()),
            () => Wrap(BuildDataTab()),
            () => Wrap(BuildAboutTab())
        };
        nav.Items =
        [
            T("settings.general"), T("settings.appearance"), T("settings.language"), T("settings.notifications"),
            T("settings.focus"), T("settings.pomodoro"), T("settings.shortcuts"), T("settings.database"),
            T("settings.data"), T("settings.about")
        ];
        void Show(int i)
        {
            host.Controls.Clear();
            var page = builders[Math.Clamp(i, 0, builders.Length - 1)]();
            page.Dock = DockStyle.Fill;
            host.Controls.Add(page);
        }
        nav.SelectedIndexChanged += (_, i) => Show(i);
        Show(0);
        var header = new PageHeader { TitleText = T("nav.settings"), SubtitleText = "System configuration" };
        ContentPanel.Controls.Add(host);
        ContentPanel.Controls.Add(nav);
        ContentPanel.Controls.Add(header);
    }

    private static Panel Wrap(TabPage page)
    {
        var panel = new CardPanel { Dock = DockStyle.Fill, AutoScroll = true };
        var children = page.Controls.Cast<Control>().Reverse().ToArray();
        page.Controls.Clear();
        foreach (var c in children)
            panel.Controls.Add(c);
        return panel;
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var settings = GetService<ISettingsService>(scope);
            _prefs = await settings.GetPreferencesAsync();
            _culture = await settings.GetSettingAsync("Culture") ?? "en-US";
            _minimizeToTray = await settings.GetSettingAsync("MinimizeToTray") == "true";
            _alwaysOnTop = await settings.GetSettingAsync("AlwaysOnTop") == "true";
            _startMinimized = await settings.GetSettingAsync("StartMinimized") == "true";
            using (var startupScope = ScopeFactory.CreateScope())
                _startWithWindows = GetService<IStartupRegistration>(startupScope).IsRegistered;
            BuildTabs();
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private TabPage BuildGeneralTab()
    {
        var page = new TabPage(T("settings.general"));
        var name = new TextField { Text = _prefs.DisplayName, Dock = DockStyle.Top };
        var save = new ModernButton { Text = T("common.save"), Width = 96, Height = 32, Dock = DockStyle.Top };
        save.Click += async (_, _) =>
        {
            _prefs.DisplayName = name.Text;
            using var scope = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(scope).SavePreferencesAsync(_prefs);
        };
        var minimizeTray = new CheckBox
        {
            Text = T("settings.minimizeToTray"),
            Dock = DockStyle.Top,
            Checked = _minimizeToTray
        };
        minimizeTray.CheckedChanged += async (_, _) =>
        {
            _minimizeToTray = minimizeTray.Checked;
            using var scope = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(scope).SetSettingAsync("MinimizeToTray", _minimizeToTray ? "true" : "false");
        };

        var alwaysOnTop = new CheckBox
        {
            Text = T("settings.alwaysOnTop"),
            Dock = DockStyle.Top,
            Checked = _alwaysOnTop
        };
        alwaysOnTop.CheckedChanged += async (_, _) =>
        {
            _alwaysOnTop = alwaysOnTop.Checked;
            using var scope = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(scope).SetSettingAsync("AlwaysOnTop", _alwaysOnTop ? "true" : "false");
            foreach (Form form in System.Windows.Forms.Application.OpenForms)
            {
                if (form is MainForm main)
                    main.ApplyAlwaysOnTopSetting(_alwaysOnTop);
            }
        };

        var startMinimized = new CheckBox
        {
            Text = T("settings.startMinimized"),
            Dock = DockStyle.Top,
            Checked = _startMinimized
        };
        startMinimized.CheckedChanged += async (_, _) =>
        {
            _startMinimized = startMinimized.Checked;
            using var scope = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(scope).SetSettingAsync("StartMinimized", _startMinimized ? "true" : "false");
        };

        var startWithWindows = new CheckBox
        {
            Text = T("settings.startWithWindows"),
            Dock = DockStyle.Top,
            Checked = _startWithWindows
        };
        startWithWindows.CheckedChanged += (_, _) =>
        {
            _startWithWindows = startWithWindows.Checked;
            using var scope = ScopeFactory.CreateScope();
            GetService<IStartupRegistration>(scope).SetEnabled(_startWithWindows);
        };

        page.Controls.Add(save);
        page.Controls.Add(startWithWindows);
        page.Controls.Add(startMinimized);
        page.Controls.Add(alwaysOnTop);
        page.Controls.Add(minimizeTray);
        page.Controls.Add(name);
        page.Controls.Add(new Label { Text = T("settings.displayName"), Dock = DockStyle.Top, Height = 20 });
        return page;
    }

    private TabPage BuildAppearanceTab()
    {
        var page = new TabPage(T("settings.appearance"));
        var theme = new ThemedComboBox { Dock = DockStyle.Top };
        foreach (ThemeMode m in Enum.GetValues(typeof(ThemeMode))) theme.Items.Add(m);
        theme.SelectedItem = _prefs.Theme;
        theme.SelectedIndexChanged += async (_, _) =>
        {
            if (theme.SelectedItem is ThemeMode m)
            {
                _prefs.Theme = m;
                ThemeManager.Instance.SetMode(m);
                using var scope = ScopeFactory.CreateScope();
                await GetService<ISettingsService>(scope).SavePreferencesAsync(_prefs);
            }
        };
        page.Controls.Add(theme);
        page.Controls.Add(new Label { Text = T("settings.theme"), Dock = DockStyle.Top, Height = 20 });
        return page;
    }

    private TabPage BuildLanguageTab()
    {
        var page = new TabPage(T("settings.language"));
        var lang = new ThemedComboBox { Dock = DockStyle.Top };
        lang.Items.AddRange(["en-US", "fa-IR"]);
        lang.SelectedItem = _culture ?? "en-US";
        if (lang.SelectedIndex < 0) lang.SelectedIndex = 0;
        lang.SelectedIndexChanged += async (_, _) =>
        {
            var culture = lang.SelectedItem?.ToString() ?? "en-US";
            _culture = culture;
            LocalizationService.Instance.SetLanguage(culture);
            using var scope = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(scope).SetSettingAsync("Culture", culture);
        };
        page.Controls.Add(lang);
        return page;
    }

    private TabPage BuildNotificationsTab()
    {
        var page = new TabPage(T("settings.notifications"));
        var enabled = new CheckBox { Text = "Enabled", Checked = _prefs.NotificationsEnabled, Dock = DockStyle.Top };
        enabled.CheckedChanged += (_, _) => _prefs.NotificationsEnabled = enabled.Checked;
        var save = new ModernButton { Text = T("common.save"), Dock = DockStyle.Bottom, Height = 36 };
        save.Click += async (_, _) =>
        {
            using var scope = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(scope).SavePreferencesAsync(_prefs);
        };
        page.Controls.Add(save);
        page.Controls.Add(enabled);
        return page;
    }

    private TabPage BuildFocusTab()
    {
        var page = new TabPage(T("settings.focus"));
        using var scope = ScopeFactory.CreateScope();
        var opts = scope.ServiceProvider.GetService<IOptions<FocusOptions>>()?.Value;
        var target = new NumericUpDown
        {
            Minimum = 30,
            Maximum = 480,
            Value = _prefs.TargetFocusMinutesPerDay,
            Dock = DockStyle.Top
        };
        target.ValueChanged += (_, _) => _prefs.TargetFocusMinutesPerDay = (int)target.Value;
        var save = new ModernButton { Text = T("common.save"), Dock = DockStyle.Bottom, Height = 36 };
        save.Click += async (_, _) =>
        {
            using var s = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(s).SavePreferencesAsync(_prefs);
        };
        page.Controls.Add(save);
        page.Controls.Add(target);
        page.Controls.Add(new Label { Text = $"Target focus min/day (default session: {opts?.DefaultSessionMinutes ?? 60} min)", Dock = DockStyle.Top, Height = 24 });
        return page;
    }

    private TabPage BuildPomodoroTab()
    {
        var page = new TabPage(T("settings.pomodoro"));
        var work = new NumericUpDown { Minimum = 5, Maximum = 90, Value = _prefs.PomodoroWorkMinutes, Dock = DockStyle.Top };
        work.ValueChanged += (_, _) => _prefs.PomodoroWorkMinutes = (int)work.Value;
        var save = new ModernButton { Text = T("common.save"), Dock = DockStyle.Bottom, Height = 36 };
        save.Click += async (_, _) =>
        {
            using var scope = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(scope).SavePreferencesAsync(_prefs);
        };
        page.Controls.Add(save);
        page.Controls.Add(work);
        page.Controls.Add(new Label { Text = "Work minutes", Dock = DockStyle.Top, Height = 20 });
        return page;
    }

    private TabPage BuildShortcutsTab()
    {
        var page = new TabPage(T("settings.shortcuts"));
        page.Controls.Add(new Label
        {
            Text = "Ctrl+K Search | Ctrl+Shift+Space Quick Add | Ctrl+N New Task | Ctrl+S Save | Ctrl+Shift+F Start Focus | F1 Help",
            Dock = DockStyle.Fill
        });
        return page;
    }

    private TabPage BuildDatabaseTab()
    {
        var page = new TabPage(T("settings.database"));
        using var scope = ScopeFactory.CreateScope();
        var dbOpts = scope.ServiceProvider.GetService<IOptions<DatabaseOptions>>()?.Value;
        page.Controls.Add(new Label
        {
            Text = $"Auto migrate: {dbOpts?.ApplyMigrationsOnStartup}\nSeed: {dbOpts?.SeedSampleData}",
            Dock = DockStyle.Fill
        });
        return page;
    }

    private TabPage BuildDataTab()
    {
        var page = new TabPage(T("settings.data"));
        var export = new ModernButton { Text = T("settings.export"), Dock = DockStyle.Top, Height = 36 };
        export.Click += async (_, _) =>
        {
            using var scope = ScopeFactory.CreateScope();
            var fs = GetService<IFileSystemService>(scope);
            var json = await GetService<IImportExportService>(scope).ExportJsonAsync();
            var path = Path.Combine(fs.GetDefaultExportDirectory(), $"devdesk-export-{DateTime.Now:yyyyMMdd}.json");
            fs.EnsureDirectory(Path.GetDirectoryName(path)!);
            await fs.WriteTextAsync(path, json);
            MessageBox.Show($"Exported to {path}");
        };
        var import = new ModernButton { Text = T("settings.import"), Dock = DockStyle.Top, Height = 36, IsPrimary = false };
        import.Click += async (_, _) =>
        {
            using var dlg = new OpenFileDialog { Filter = "JSON|*.json" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            using var scope = ScopeFactory.CreateScope();
            var fs = GetService<IFileSystemService>(scope);
            var json = await fs.ReadTextAsync(dlg.FileName);
            var result = await GetService<IImportExportService>(scope).ImportJsonAsync(json);
            MessageBox.Show(result.Message);
        };
        var backup = new ModernButton { Text = T("settings.backup"), Dock = DockStyle.Top, Height = 36, IsPrimary = false };
        backup.Click += async (_, _) =>
        {
            using var scope = ScopeFactory.CreateScope();
            var fs = GetService<IFileSystemService>(scope);
            var dir = fs.GetDefaultBackupDirectory();
            fs.EnsureDirectory(dir);
            var path = Path.Combine(dir, $"devdesk-{DateTime.Now:yyyyMMdd-HHmmss}.bak");
            await GetService<IDatabaseBackupService>(scope).BackupAsync(path);
            MessageBox.Show($"Backup saved to {path}");
        };
        page.Controls.Add(backup);
        page.Controls.Add(import);
        page.Controls.Add(export);
        return page;
    }

    private TabPage BuildAboutTab()
    {
        var page = new TabPage(T("settings.about"));
        page.Controls.Add(new Label { Text = "DevDesk v1.0 — Developer Productivity", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
        return page;
    }
}
