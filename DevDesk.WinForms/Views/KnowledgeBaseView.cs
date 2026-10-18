using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Common;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class KnowledgeBaseView : ViewBase
{
    private readonly ThemedComboBox _category = new() { Dock = DockStyle.Top };
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };

    public KnowledgeBaseView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _category.Items.Add(T("common.all"));
        foreach (var c in KnowledgeCategories.All) _category.Items.Add(c);
        _category.SelectedIndex = 0;
        _category.SelectedIndexChanged += async (_, _) => await FilterAsync();

        var header = new PageHeader { TitleText = T("nav.knowledge") };
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_category);
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
            var notes = await GetService<INoteService>(scope).GetAllAsync(knowledgeBaseOnly: true);
            ApplyFilter(notes);
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async Task FilterAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var notes = await GetService<INoteService>(scope).GetAllAsync(knowledgeBaseOnly: true);
        ApplyFilter(notes);
    }

    private void ApplyFilter(IReadOnlyList<NoteDto> notes)
    {
        var cat = _category.SelectedItem?.ToString();
        var filtered = cat is null or "All" or "همه"
            ? notes
            : notes.Where(n => n.KnowledgeCategory == cat).ToList();

        _list.Controls.Clear();
        var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));

        if (filtered.Count == 0)
        {
            var empty = new EmptyStatePanel
            {
                TitleText = T("common.noData"),
                Icon = "menu_book",
                Width = targetW,
                Height = UiScale.Px(200)
            };
            _list.Controls.Add(empty);
            return;
        }

        foreach (var n in filtered)
        {
            var row = new InventoryRow
            {
                Width = targetW,
                Margin = new Padding(0, 0, 0, UiMetrics.Space8),
                Item = n
            };
            row.Bind(n.Title, n.KnowledgeCategory);
            row.Activated += (_, _) => Navigation.Navigate("note-editor", n.Id);
            row.Click += (_, _) =>
            {
                foreach (Control c in _list.Controls)
                {
                    if (c is InventoryRow r)
                        r.Selected = ReferenceEquals(r.Item, n);
                }
            };
            _list.Controls.Add(row);
        }
    }
}
