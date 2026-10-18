using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class GoalsView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly CardPanel _editorCard = new() { Dock = DockStyle.Bottom, Height = UiScale.Px(72), Visible = false, Margin = new Padding(0, UiMetrics.Space8, 0, 0) };
    private readonly Label _goalTitle = new() { Dock = DockStyle.Top, Height = UiMetrics.LineTitle, Font = UiMetrics.SectionTitle };
    private readonly Label _progressLabel = new() { Dock = DockStyle.Right, Width = UiScale.Px(60), Font = UiMetrics.StatValue, TextAlign = ContentAlignment.MiddleRight };
    private readonly TrackBar _progress = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100 };
    private readonly ModernButton _add = new() { AutoFit = true };
    private readonly ModernButton _delete = new() { IsPrimary = false, AutoFit = true };
    private Application.Dtos.GoalDto? _selected;

    public GoalsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _add.Text = T("goals.add");
        _delete.Text = T("common.delete");
        _add.FitToContents();
        _delete.FitToContents();

        _add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), $"{T("goals.goal")}:");
            if (string.IsNullOrWhiteSpace(title)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IGoalService>(scope).CreateAsync(new Application.Dtos.CreateGoalRequest { Title = title });
            await LoadAsync();
        };

        _delete.Click += async (_, _) => await DeleteSelectedAsync();

        var sliderPanel = new Panel { Dock = DockStyle.Fill, Tag = "no-theme" };
        sliderPanel.Controls.Add(_progress);
        sliderPanel.Controls.Add(_progressLabel);

        _progress.ValueChanged += async (_, _) =>
        {
            if (_selected is null) return;
            _progressLabel.Text = $"{_progress.Value}%";
            using var scope = ScopeFactory.CreateScope();
            await GetService<IGoalService>(scope).SetProgressAsync(_selected.Id, _progress.Value);
            _selected.Progress = _progress.Value;
            UpdateRowProgress(_selected.Id, _progress.Value);
        };

        _editorCard.Controls.Add(sliderPanel);
        _editorCard.Controls.Add(_goalTitle);

        var header = new PageHeader { TitleText = T("nav.goals") };
        header.Actions.Controls.Add(_add);
        header.Actions.Controls.Add(_delete);

        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_editorCard);
        ContentPanel.Controls.Add(header);

        _list.Resize += (_, _) =>
        {
            var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));
            foreach (Control c in _list.Controls)
                c.Width = targetW;
        };

        ApplyThemeStyles();
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyThemeStyles();
    }

    private void ApplyThemeStyles()
    {
        var c = ThemeManager.Instance.Current;
        var isRtl = LocalizationService.Instance.IsRtl;
        _goalTitle.ForeColor = c.TextPrimary;
        _goalTitle.TextAlign = isRtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        _progressLabel.ForeColor = c.Accent;
        _progressLabel.Dock = isRtl ? DockStyle.Left : DockStyle.Right;
        _progressLabel.TextAlign = isRtl ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;
    }

    private void UpdateRowProgress(Guid goalId, int progress)
    {
        foreach (Control c in _list.Controls)
        {
            if (c is InventoryRow r && r.Item is Application.Dtos.GoalDto g && g.Id == goalId)
            {
                r.Bind(g.Title, $"{progress}%");
                break;
            }
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (_selected is null) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IGoalService>(scope).DeleteAsync(_selected.Id);
        _selected = null;
        _editorCard.Visible = false;
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var goals = await GetService<IGoalService>(scope).GetAllAsync();
            _list.Controls.Clear();
            _selected = null;
            _editorCard.Visible = false;

            if (goals.Count == 0)
            {
                var empty = new EmptyStatePanel
                {
                    TitleText = T("goals.empty"),
                    Icon = "flag",
                    Width = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16)),
                    Height = UiScale.Px(200)
                };
                _list.Controls.Add(empty);
            }
            else
            {
                var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));
                foreach (var g in goals)
                {
                    var row = new InventoryRow
                    {
                        Width = targetW,
                        Margin = new Padding(0, 0, 0, UiMetrics.Space8),
                        Item = g
                    };
                    row.Bind(g.Title, $"{g.Progress}%");
                    row.Click += (_, _) =>
                    {
                        _selected = g;
                        _goalTitle.Text = g.Title;
                        _progress.Value = Math.Clamp(g.Progress, 0, 100);
                        _progressLabel.Text = $"{g.Progress}%";
                        _editorCard.Visible = true;
                        foreach (Control c in _list.Controls)
                        {
                            if (c is InventoryRow r)
                                r.Selected = ReferenceEquals(r.Item, g);
                        }
                    };
                    _list.Controls.Add(row);
                }
            }

            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
