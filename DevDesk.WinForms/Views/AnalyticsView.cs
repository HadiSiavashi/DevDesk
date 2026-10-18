using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
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
        var isRtl = LocalizationService.Instance.IsRtl;
        var header = new PageHeader { TitleText = T("nav.analytics"), SubtitleText = isRtl ? "بینش‌های بهره‌وری" : "Productivity insights" };
        _summary.Font = UiMetrics.Body;
        _summary.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        ContentPanel.Controls.Add(_charts);
        ContentPanel.Controls.Add(_summary);
        ContentPanel.Controls.Add(header);

        _charts.Resize += (_, _) =>
        {
            var targetW = Math.Max(UiScale.Px(480), _charts.ClientSize.Width - UiScale.Px(24));
            foreach (Control c in _charts.Controls)
                c.Width = targetW;
        };
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
            var isRtl = LocalizationService.Instance.IsRtl;
            _summary.Text =
                $"{T("analytics.tasksCompleted")}: {data.TotalTasksCompleted}    {T("analytics.focusMinutes")}: {data.TotalFocusMinutes}    {T("analytics.avgScore")}: {data.AverageProductivityScore:F1}\r\n" +
                $"{T("analytics.estimatedMinutes")}: {data.TotalEstimatedMinutes}    {T("analytics.actualMinutes")}: {data.TotalActualMinutes}";
            _summary.ForeColor = ThemeManager.Instance.Current.TextSecondary;
            _charts.Controls.Clear();
            _charts.Controls.Add(new ChartCard(isRtl ? "وظایف / روز" : "Tasks/Day", data.TasksCompletedPerDay));
            _charts.Controls.Add(new ChartCard(isRtl ? "دقایق تمرکز / روز" : "Focus Min/Day", data.FocusMinutesPerDay));
            _charts.Controls.Add(new ChartCard(T("analytics.focusByProject"), data.FocusByProject));
            _charts.Controls.Add(new ChartCard(isRtl ? "بر اساس وضعیت" : "By Status", data.TasksByStatus));
            _charts.Controls.Add(new ChartCard(isRtl ? "بر اساس اولویت" : "By Priority", data.TasksByPriority));
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
            Width = Math.Max(UiScale.Px(480), Parent?.ClientSize.Width - 24 ?? UiScale.Px(720));
            Height = UiScale.Px(48) + Math.Max(1, points.Count) * UiScale.Px(32);
            Margin = new Padding(0, 0, 0, UiMetrics.Space12);
            Paint += OnChartPaint;
            Resize += (_, _) => Invalidate();
        }

        private void OnChartPaint(object? sender, PaintEventArgs e)
        {
            var c = ThemeManager.Instance.Current;
            var g = e.Graphics;
            var isRtl = LocalizationService.Instance.IsRtl;
            var titleFlags = (isRtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter;
            TextRenderer.DrawText(g, _title, UiMetrics.SectionTitle, new Rectangle(UiMetrics.Space16, UiMetrics.Space12, Width - UiMetrics.Space32, UiMetrics.LineTitle), c.TextPrimary, titleFlags);

            if (_points.Count == 0)
            {
                TextRenderer.DrawText(g, isRtl ? "داده‌ای موجود نیست" : "No data", UiMetrics.Meta, new Rectangle(UiMetrics.Space16, UiScale.Px(40), Width - UiMetrics.Space32, UiMetrics.LineBody), c.TextMuted, titleFlags);
                return;
            }

            var max = Math.Max(1, _points.Max(p => p.Value));
            var labelW = UiScale.Px(140);
            var barMax = Math.Max(UiScale.Px(40), Width - labelW - UiScale.Px(72));
            var y = UiScale.Px(44);
            var rowH = UiScale.Px(28);

            var labelFlags = (isRtl ? TextFormatFlags.Right : TextFormatFlags.Left)
                           | TextFormatFlags.VerticalCenter
                           | TextFormatFlags.EndEllipsis
                           | TextFormatFlags.NoPadding;

            foreach (var p in _points)
            {
                var w = Math.Max(p.Value <= 0 ? 2 : 8, (int)(barMax * (p.Value / max)));
                Rectangle labelRect;
                Rectangle bar;

                if (isRtl)
                {
                    labelRect = new Rectangle(Width - UiMetrics.Space16 - labelW, y, labelW, rowH);
                    var barLeft = Width - UiMetrics.Space16 - labelW - UiMetrics.Space12 - w;
                    bar = new Rectangle(barLeft, y + (rowH - UiMetrics.ProgressHeight) / 2, w, UiMetrics.ProgressHeight);
                }
                else
                {
                    labelRect = new Rectangle(UiMetrics.Space16, y, labelW, rowH);
                    var barLeft = UiMetrics.Space16 + labelW;
                    bar = new Rectangle(barLeft, y + (rowH - UiMetrics.ProgressHeight) / 2, w, UiMetrics.ProgressHeight);
                }

                TextRenderer.DrawText(g, $"{p.Label}: {p.Value:F0}", UiMetrics.Meta, labelRect, c.TextSecondary, labelFlags);

                using var fill = new SolidBrush(c.Accent);
                DrawingUtil.FillRounded(g, fill, bar, UiMetrics.RadiusSm);
                y += rowH + UiMetrics.Space4;
            }
        }
    }
}
