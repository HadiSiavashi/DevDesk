using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class AnalyticsView : ViewBase
{
    private readonly FlowLayoutPanel _charts = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Tag = "no-theme"
    };
    private readonly Label _summary = new() { Dock = DockStyle.Top, Height = 56, AutoSize = false };

    public AnalyticsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        var header = new PageHeader { TitleText = T("nav.analytics"), SubtitleText = "Productivity insights" };
        _summary.Font = UiMetrics.Body;
        ContentPanel.Controls.Add(_charts);
        ContentPanel.Controls.Add(_summary);
        ContentPanel.Controls.Add(header);
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
                $"{T("analytics.tasksCompleted")}: {data.TotalTasksCompleted}    {T("analytics.focusMinutes")}: {data.TotalFocusMinutes}    {T("analytics.avgScore")}: {data.AverageProductivityScore:F1}\r\n" +
                $"{T("analytics.estimatedMinutes")}: {data.TotalEstimatedMinutes}    {T("analytics.actualMinutes")}: {data.TotalActualMinutes}";
            _summary.ForeColor = ThemeManager.Instance.Current.TextSecondary;
            _charts.Controls.Clear();
            _charts.Controls.Add(new ChartCard("Tasks/Day", data.TasksCompletedPerDay));
            _charts.Controls.Add(new ChartCard("Focus Min/Day", data.FocusMinutesPerDay));
            _charts.Controls.Add(new ChartCard(T("analytics.focusByProject"), data.FocusByProject));
            _charts.Controls.Add(new ChartCard("By Status", data.TasksByStatus));
            _charts.Controls.Add(new ChartCard("By Priority", data.TasksByPriority));
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private sealed class ChartCard : CardPanel
    {
        private readonly string _title;
        private readonly IReadOnlyList<Application.Dtos.ChartPointDto> _points;

        public ChartCard(string title, IReadOnlyList<Application.Dtos.ChartPointDto> points)
        {
            _title = title;
            _points = points;
            Width = 720;
            Height = 56 + Math.Max(1, points.Count) * 28;
            Margin = new Padding(0, 0, 0, 12);
            Paint += OnChartPaint;
            Resize += (_, _) => Invalidate();
        }

        private void OnChartPaint(object? sender, PaintEventArgs e)
        {
            var c = ThemeManager.Instance.Current;
            var g = e.Graphics;
            TextRenderer.DrawText(g, _title, UiMetrics.SectionTitle, new Rectangle(16, 12, Width - 32, 22), c.TextPrimary);
            if (_points.Count == 0)
            {
                TextRenderer.DrawText(g, "No data", UiMetrics.Meta, new Rectangle(16, 40, Width - 32, 20), c.TextMuted);
                return;
            }

            var max = Math.Max(1, _points.Max(p => p.Value));
            var labelW = 120;
            var barLeft = 16 + labelW;
            var barMax = Math.Max(40, Width - barLeft - 48);
            var y = 42;
            foreach (var p in _points)
            {
                var row = new Rectangle(16, y, Width - 32, 24);
                TextRenderer.DrawText(g, $"{p.Label}: {p.Value:F0}", UiMetrics.Meta, new Rectangle(16, y, labelW, 24), c.TextSecondary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                var w = Math.Max(p.Value <= 0 ? 2 : 8, (int)(barMax * (p.Value / max)));
                var bar = new Rectangle(barLeft, y + 7, w, 10);
                using var fill = new SolidBrush(c.Accent);
                DrawingUtil.FillRounded(g, fill, bar, 3);
                y += 28;
            }
            _ = y;
        }
    }
}
