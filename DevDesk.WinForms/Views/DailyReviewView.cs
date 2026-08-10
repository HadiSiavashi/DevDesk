using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class DailyReviewView : ViewBase
{
    private readonly TextBox _well = new() { Dock = DockStyle.Top, Height = 60, Multiline = true };
    private readonly TextBox _notWell = new() { Dock = DockStyle.Top, Height = 60, Multiline = true };
    private readonly TextBox _lessons = new() { Dock = DockStyle.Top, Height = 60, Multiline = true };
    private readonly TextBox _tomorrow = new() { Dock = DockStyle.Top, Height = 60, Multiline = true };
    private readonly ModernButton _save = new() { Dock = DockStyle.Bottom, Height = 36, Text = "Save" };

    public DailyReviewView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _save.Click += async (_, _) => await SaveAsync();
        ContentPanel.Controls.Add(_save);
        ContentPanel.Controls.Add(_tomorrow);
        ContentPanel.Controls.Add(new Label { Text = T("dailyreview.tomorrow"), Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_lessons);
        ContentPanel.Controls.Add(new Label { Text = T("dailyreview.lessons"), Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_notWell);
        ContentPanel.Controls.Add(new Label { Text = T("dailyreview.notWell"), Dock = DockStyle.Top, Height = 20 });
        ContentPanel.Controls.Add(_well);
        ContentPanel.Controls.Add(new Label { Text = T("dailyreview.wentWell"), Dock = DockStyle.Top, Height = 20 });
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
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
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
