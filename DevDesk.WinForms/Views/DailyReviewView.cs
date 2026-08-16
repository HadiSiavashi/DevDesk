using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class DailyReviewView : ViewBase
{
    private readonly TextBox _well = new() { Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.None };
    private readonly TextBox _notWell = new() { Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.None };
    private readonly TextBox _lessons = new() { Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.None };
    private readonly TextBox _tomorrow = new() { Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.None };
    private readonly ModernButton _save = new() { Height = UiMetrics.ButtonHeight, Text = "Save", Width = 88 };

    public DailyReviewView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _save.Click += async (_, _) => await SaveAsync();
        var header = new PageHeader { TitleText = T("nav.dailyreview"), SubtitleText = DateTime.Today.ToString("dddd, MMMM d") };
        header.Actions.Controls.Add(_save);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Tag = "no-theme"
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.Controls.Add(MakePromptCard(T("dailyreview.wentWell"), _well), 0, 0);
        grid.Controls.Add(MakePromptCard(T("dailyreview.notWell"), _notWell), 1, 0);
        grid.Controls.Add(MakePromptCard(T("dailyreview.lessons"), _lessons), 0, 1);
        grid.Controls.Add(MakePromptCard(T("dailyreview.tomorrow"), _tomorrow), 1, 1);

        ContentPanel.Controls.Add(grid);
        ContentPanel.Controls.Add(header);
    }

    private static CardPanel MakePromptCard(string title, TextBox input)
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 8) };
        input.Font = UiMetrics.Body;
        card.Controls.Add(input);
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 24, Font = UiMetrics.SectionTitle });
        return card;
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var review = await GetService<IDailyReviewService>(scope).GetOrCreateAsync(DateOnly.FromDateTime(DateTime.Today));
            _well.Text = review.WhatWentWell ?? "";
            _notWell.Text = review.WhatDidNotGoWell ?? "";
            _lessons.Text = review.LessonsLearned ?? "";
            _tomorrow.Text = review.TomorrowPlan ?? "";
            ApplyTheme();
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        foreach (var box in new[] { _well, _notWell, _lessons, _tomorrow })
        {
            box.BackColor = c.Surface;
            box.ForeColor = c.TextPrimary;
            box.Font = UiMetrics.Body;
        }
    }

    protected override void OnThemeChanged()
    {
        base.OnThemeChanged();
        ApplyTheme();
    }

    private async Task SaveAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        await GetService<IDailyReviewService>(scope).UpdateAsync(DateOnly.FromDateTime(DateTime.Today), new Application.Dtos.UpdateDailyReviewRequest
        {
            WhatWentWell = _well.Text,
            WhatDidNotGoWell = _notWell.Text,
            LessonsLearned = _lessons.Text,
            TomorrowPlan = _tomorrow.Text
        });
    }
}
