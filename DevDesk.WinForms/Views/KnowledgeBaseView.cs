using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Common;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class KnowledgeBaseView : ViewBase
{
    private readonly ComboBox _category = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
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
        foreach (var n in filtered)
        {
            var row = new InventoryRow { Width = Math.Max(280, _list.ClientSize.Width - 8), Margin = new Padding(0, 0, 0, 8) };
            row.Bind(n.Title, n.KnowledgeCategory);
            row.Activated += (_, _) => Navigation.Navigate("note-editor", n.Id);
            _list.Controls.Add(row);
        }
    }
}
