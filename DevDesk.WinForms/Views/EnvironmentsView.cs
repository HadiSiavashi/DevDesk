using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class EnvironmentsView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ThemedComboBox _projects = new() { Dock = DockStyle.Top };
    private readonly ModernButton _add = new() { AutoFit = true };

    public EnvironmentsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _add.Text = T("environments.add");
        _add.FitToContents();
        _projects.SelectedIndexChanged += async (_, _) => await LoadEnvsAsync();
        _add.Click += async (_, _) =>
        {
            if (_projects.SelectedItem is not ProjectListItemDto p) return;
            var name = Dialogs.InputDialog.Show(T("common.create"), $"{T("common.name")}:");
            if (string.IsNullOrWhiteSpace(name)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IEnvironmentService>(scope).CreateAsync(new CreateEnvironmentRequest
            {
                ProjectId = p.Id,
                Name = name,
                EnvironmentType = EnvironmentType.Development
            });
            await LoadEnvsAsync();
        };

        var header = new PageHeader { TitleText = T("nav.environments") };
        header.Actions.Controls.Add(_add);
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_projects);
        ContentPanel.Controls.Add(header);

        _list.Resize += (_, _) =>
        {
            var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));
            foreach (Control c in _list.Controls)
                c.Width = targetW;
        };
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var projects = await GetService<IProjectService>(scope).GetAllAsync();
            var list = projects.ToList();
            _projects.DataSource = list;
            _projects.DisplayMember = "Name";
            _add.Enabled = list.Count > 0;
            await LoadEnvsAsync();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async Task LoadEnvsAsync()
    {
        _list.Controls.Clear();
        var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));

        if (_projects.SelectedItem is not ProjectListItemDto p)
        {
            var empty = new EmptyStatePanel
            {
                TitleText = T("environments.noProjects"),
                Icon = "developer_board",
                Width = targetW,
                Height = UiScale.Px(200)
            };
            _list.Controls.Add(empty);
            ShowContent();
            return;
        }

        using var scope = ScopeFactory.CreateScope();
        var envs = await GetService<IEnvironmentService>(scope).GetByProjectAsync(p.Id);
        if (envs.Count == 0)
        {
            var empty = new EmptyStatePanel
            {
                TitleText = T("environments.empty"),
                Icon = "developer_board",
                Width = targetW,
                Height = UiScale.Px(200)
            };
            _list.Controls.Add(empty);
        }
        else
        {
            foreach (var e in envs)
            {
                var row = new InventoryRow
                {
                    Width = targetW,
                    Margin = new Padding(0, 0, 0, UiMetrics.Space8),
                    Item = e
                };
                row.Bind(e.Name, e.EnvironmentType.ToString());
                _list.Controls.Add(row);
            }
        }

        ShowContent();
    }
}
