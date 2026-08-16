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
        var nav = new Panel { Dock = DockStyle.Left, Width = 240, Tag = "no-theme", Padding = new Padding(8) };
        var host = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24, 8, 24, 24), Tag = "no-theme" };
        var sections = new (string Title, Func<Panel> Build)[]
        {
            (T("settings.general"), () => Wrap(BuildGeneralTab())),
            (T("settings.appearance"), () => Wrap(BuildAppearanceTab())),
            (T("settings.language"), () => Wrap(BuildLanguageTab())),
            (T("settings.notifications"), () => Wrap(BuildNotificationsTab())),
            (T("settings.focus"), () => Wrap(BuildFocusTab())),
            (T("settings.pomodoro"), () => Wrap(BuildPomodoroTab())),
            (T("settings.shortcuts"), () => Wrap(BuildShortcutsTab())),
            (T("settings.database"), () => Wrap(BuildDatabaseTab())),
            (T("settings.data"), () => Wrap(BuildDataTab())),
            (T("settings.about"), () => Wrap(BuildAboutTab()))
        };
        var y = 8;
        Panel? current = null;
        for (var i = 0; i < sections.Length; i++)
        {
            var (title, build) = sections[i];
            var btn = new ModernButton
            {
                Text = title,
                Variant = i == 0 ? ButtonVariant.Primary : ButtonVariant.Ghost,
                Width = 216,
                Height = 32,
                Left = 8,
                Top = y
            };
            y += 36;
            btn.Click += (_, _) =>
            {
                foreach (Control c in nav.Controls)
                    if (c is ModernButton mb) mb.Variant = ButtonVariant.Ghost;
                btn.Variant = ButtonVariant.Primary;
                host.Controls.Clear();
                current = build();
                current.Dock = DockStyle.Fill;
                host.Controls.Add(current);
            };
            nav.Controls.Add(btn);
        }
        current = Wrap(BuildGeneralTab());
        current.Dock = DockStyle.Fill;
        host.Controls.Add(current);
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
        var name = new TextBox { Text = _prefs.DisplayName, Dock = DockStyle.Top };
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

        var save = new ModernButton { Text = T("common.save"), Dock = DockStyle.Bottom, Height = 36 };
        save.Click += async (_, _) =>
        {
            _prefs.DisplayName = name.Text;
            using var scope = ScopeFactory.CreateScope();
            await GetService<ISettingsService>(scope).SavePreferencesAsync(_prefs);
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
        var theme = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
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
        var lang = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
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
