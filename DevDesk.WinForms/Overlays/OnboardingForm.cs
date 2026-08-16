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
    private readonly TextField _name = new() { Dock = DockStyle.Top };
    private readonly ComboBox _language = new() { Dock = DockStyle.Top, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _theme = new() { Dock = DockStyle.Top, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _workHours = new() { Minimum = 4, Maximum = 12, Value = 8, Dock = DockStyle.Top, Height = 28 };
    private readonly NumericUpDown _pomodoro = new() { Minimum = 15, Maximum = 60, Value = 25, Dock = DockStyle.Top, Height = 28 };
    private readonly Panel _header = new() { Dock = DockStyle.Top, Height = 72, Tag = "no-theme" };
    private readonly Label _subtitle = new() { Dock = DockStyle.Fill, Font = UiMetrics.Body };
    private readonly Label _title = new() { Dock = DockStyle.Top, Height = 32, Font = UiMetrics.PageTitle };

    public OnboardingForm(IServiceProvider services)
    {
        _services = services;
        Text = LocalizationService.Instance.Get("onboarding.welcome");
        Size = new Size(480, 480);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Padding = new Padding(24);

        _language.Items.AddRange(["en-US", "fa-IR"]);
        _language.SelectedIndex = 0;
        foreach (ThemeMode m in Enum.GetValues(typeof(ThemeMode))) _theme.Items.Add(m);
        _theme.SelectedItem = ThemeMode.System;

        _title.Text = LocalizationService.Instance.Get("onboarding.welcome");
        _subtitle.Text = LocalizationService.Instance.Get("onboarding.subtitle");
        _header.Controls.Add(_subtitle);
        _header.Controls.Add(_title);

        var fields = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8), Tag = "no-theme" };
        AddField(fields, LocalizationService.Instance.Get("settings.displayName"), _name);
        AddField(fields, LocalizationService.Instance.Get("settings.language"), _language);
        AddField(fields, LocalizationService.Instance.Get("settings.theme"), _theme);
        AddField(fields, "Work hours/day", _workHours);
        AddField(fields, "Pomodoro (min)", _pomodoro);

        var start = new ModernButton
        {
            Text = LocalizationService.Instance.Get("onboarding.getStarted"),
            Dock = DockStyle.Bottom,
            Height = 40
        };
        start.Click += async (_, _) =>
        {
            start.Enabled = false;
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
                start.Enabled = true;
            }
        };

        Controls.Add(start);
        Controls.Add(fields);
        Controls.Add(_header);
        ThemeManager.Instance.ApplyTo(this);
        ThemeManager.Instance.ThemeChanged += (_, _) => StyleHeader();
        StyleHeader();
    }

    private static void AddField(Control parent, string label, Control input)
    {
        var lbl = new Label { Text = label, Dock = DockStyle.Top, Height = 20, Font = UiMetrics.Meta };
        input.Margin = new Padding(0, 0, 0, 12);
        parent.Controls.Add(input);
        parent.Controls.Add(lbl);
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
        prefs.DisplayName = _name.Text ?? "";
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
