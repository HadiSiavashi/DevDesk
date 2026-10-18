using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Overlays;

public sealed class OnboardingForm : Form
{
    private readonly IServiceProvider _services;
    private readonly TextField _name = new();
    private readonly ThemedComboBox _language = new();
    private readonly ThemedComboBox _theme = new();
    private readonly NumericUpDown _workHours = new() { Minimum = 4, Maximum = 16, Value = 8, Height = 28 };
    private readonly NumericUpDown _pomodoro = new() { Minimum = 15, Maximum = 60, Value = 25, Height = 28 };
    private readonly Panel _header = new() { Dock = DockStyle.Top, Height = 76, Tag = "no-theme" };
    private readonly Label _subtitle = new() { Dock = DockStyle.Fill, Font = UiMetrics.Body };
    private readonly Label _title = new() { Dock = DockStyle.Top, Height = 34, Font = UiMetrics.PageTitle };
    private readonly ModernButton _start = new() { Height = 40, Dock = DockStyle.Bottom };

    private readonly Label _lblDisplayName = new() { Font = UiMetrics.Meta, Height = 18 };
    private readonly Label _lblLanguage = new() { Font = UiMetrics.Meta, Height = 18 };
    private readonly Label _lblTheme = new() { Font = UiMetrics.Meta, Height = 18 };
    private readonly Label _lblWorkHours = new() { Font = UiMetrics.Meta, Height = 18 };
    private readonly Label _lblPomodoro = new() { Font = UiMetrics.Meta, Height = 18 };

    public OnboardingForm(IServiceProvider services)
    {
        _services = services;
        Text = LocalizationService.Instance.Get("onboarding.welcome");
        Size = new Size(500, 530);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Padding = new Padding(24);

        _language.Items.AddRange(["en-US", "fa-IR"]);
        _language.SelectedItem = LocalizationService.Instance.CurrentCulture.TwoLetterISOLanguageName.Equals("fa", StringComparison.OrdinalIgnoreCase) ? "fa-IR" : "en-US";

        foreach (ThemeMode m in Enum.GetValues(typeof(ThemeMode))) _theme.Items.Add(m);
        _theme.SelectedItem = ThemeMode.System;

        _name.PlaceholderText = "e.g. Alex or Developer";

        _header.Controls.Add(_subtitle);
        _header.Controls.Add(_title);

        var fields = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Tag = "no-theme",
            Padding = new Padding(0, 10, 0, 10)
        };

        var rowWidth = ClientSize.Width - Padding.Horizontal - 24;

        Panel CreateFieldRow(Label lbl, Control input)
        {
            var p = new Panel { Width = rowWidth, Height = input.Height + 24, Margin = new Padding(0, 0, 0, 8) };
            lbl.Dock = DockStyle.Top;
            lbl.ForeColor = ThemeManager.Instance.Current.TextSecondary;
            input.Dock = DockStyle.Bottom;
            p.Controls.Add(input);
            p.Controls.Add(lbl);
            return p;
        }

        fields.Controls.Add(CreateFieldRow(_lblDisplayName, _name));
        fields.Controls.Add(CreateFieldRow(_lblLanguage, _language));
        fields.Controls.Add(CreateFieldRow(_lblTheme, _theme));
        fields.Controls.Add(CreateFieldRow(_lblWorkHours, _workHours));
        fields.Controls.Add(CreateFieldRow(_lblPomodoro, _pomodoro));

        _start.Click += async (_, _) =>
        {
            _start.Enabled = false;
            try
            {
                await SaveAsync();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, LocalizationService.Instance.Get("error.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _start.Enabled = true;
            }
        };

        _language.SelectedIndexChanged += (_, _) =>
        {
            var culture = _language.SelectedItem?.ToString() ?? "en-US";
            LocalizationService.Instance.SetLanguage(culture);
            UpdateLanguageStrings();
        };

        _theme.SelectedIndexChanged += (_, _) =>
        {
            if (_theme.SelectedItem is ThemeMode tm)
                ThemeManager.Instance.SetMode(tm);
        };

        Controls.Add(fields);
        Controls.Add(_start);
        Controls.Add(_header);

        ThemeManager.Instance.ApplyTo(this);
        ThemeManager.Instance.ThemeChanged += (_, _) => StyleHeader();
        StyleHeader();
        UpdateLanguageStrings();
    }

    private void UpdateLanguageStrings()
    {
        var isRtl = LocalizationService.Instance.IsRtl;
        RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;

        _title.Text = LocalizationService.Instance.Get("onboarding.welcome");
        _subtitle.Text = LocalizationService.Instance.Get("onboarding.subtitle");
        _start.Text = LocalizationService.Instance.Get("onboarding.getStarted");

        _lblDisplayName.Text = LocalizationService.Instance.Get("settings.displayName");
        _lblLanguage.Text = LocalizationService.Instance.Get("settings.language");
        _lblTheme.Text = LocalizationService.Instance.Get("settings.theme");
        _lblWorkHours.Text = isRtl ? "ساعات کاری در روز" : "Work hours/day";
        _lblPomodoro.Text = isRtl ? "مدت هر پومودورو (دقیقه)" : "Pomodoro (min)";

        _title.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _subtitle.TextAlign = isRtl ? ContentAlignment.TopRight : ContentAlignment.TopLeft;
        _lblDisplayName.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _lblLanguage.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _lblTheme.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _lblWorkHours.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _lblPomodoro.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
    }

    private void StyleHeader()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.Overlay;
        _header.BackColor = c.Overlay;
        _title.ForeColor = c.TextPrimary;
        _subtitle.ForeColor = c.TextSecondary;
    }

    private async Task SaveAsync()
    {
        using var scope = _services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var prefs = await settings.GetPreferencesAsync();
        prefs.DisplayName = string.IsNullOrWhiteSpace(_name.Text) ? "Developer" : _name.Text.Trim();
        if (_theme.SelectedItem is ThemeMode tm) prefs.Theme = tm;
        prefs.PomodoroWorkMinutes = (int)_pomodoro.Value;
        prefs.DefaultAvailableWorkMinutes = (int)_workHours.Value * 60;
        await settings.SavePreferencesAsync(prefs);
        var culture = _language.SelectedItem?.ToString() ?? "en-US";
        await settings.SetSettingAsync("Culture", culture);
        await settings.SetSettingAsync("OnboardingCompleted", "true");
        LocalizationService.Instance.SetLanguage(culture);
        ThemeManager.Instance.SetMode(prefs.Theme);
    }
}
