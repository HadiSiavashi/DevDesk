using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class AnalyticsView : ViewBase
{
    private readonly FlowLayoutPanel _charts = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly Label _summary = new() { Dock = DockStyle.Top, Height = 80 };

    public AnalyticsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        ContentPanel.Controls.Add(_charts);
        ContentPanel.Controls.Add(_summary);
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var to = DateOnly.FromDateTime(DateTime.Today);
            var from = to.AddDays(-13);
            var data = await GetService<IAnalyticsService>(scope).GetAsync(from, to);
            _summary.Text =
                $"{T("analytics.tasksCompleted")}: {data.TotalTasksCompleted} | " +
                $"{T("analytics.focusMinutes")}: {data.TotalFocusMinutes} | " +
                $"{T("analytics.avgScore")}: {data.AverageProductivityScore:F1}\r\n" +
                $"{T("analytics.estimatedMinutes")}: {data.TotalEstimatedMinutes} | " +
                $"{T("analytics.actualMinutes")}: {data.TotalActualMinutes}";
            _charts.Controls.Clear();
            _charts.Controls.Add(CreateBarPanel("Tasks/Day", data.TasksCompletedPerDay));
            _charts.Controls.Add(CreateBarPanel("Focus Min/Day", data.FocusMinutesPerDay));
            _charts.Controls.Add(CreateBarPanel(T("analytics.focusByProject"), data.FocusByProject));
            _charts.Controls.Add(CreateBarPanel("By Status", data.TasksByStatus));
            _charts.Controls.Add(CreateBarPanel("By Priority", data.TasksByPriority));
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private Panel CreateBarPanel(string title, IReadOnlyList<Application.Dtos.ChartPointDto> points)
    {
        var panel = new Panel { Width = 600, Height = 40 + points.Count * 24, Padding = new Padding(8) };
        panel.BackColor = ThemeManager.Instance.Current.Surface;
        var lbl = new Label { Text = title, Dock = DockStyle.Top, Height = 24, ForeColor = ThemeManager.Instance.Current.TextPrimary };
        panel.Controls.Add(lbl);
        var max = points.Count > 0 ? points.Max(p => p.Value) : 1;
        if (max <= 0) max = 1;
        var y = 28;
        foreach (var p in points)
        {
            var barW = (int)(400 * (p.Value / max));
            var bar = new Panel { Left = 120, Top = y, Width = Math.Max(2, barW), Height = 18, BackColor = ThemeManager.Instance.Current.Accent };
            var plbl = new Label { Text = $"{p.Label}: {p.Value:F0}", Left = 8, Top = y, Width = 110, ForeColor = ThemeManager.Instance.Current.TextSecondary };
            panel.Controls.Add(bar);
            panel.Controls.Add(plbl);
            y += 22;
        }
        return panel;
    }
}
