using DevDesk.Application.Interfaces;
using DevDesk.Domain.Common;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class KnowledgeBaseView : ViewBase
{
    private readonly ComboBox _category = new() { Dock = DockStyle.Top };
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };

    public KnowledgeBaseView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _category.Items.Add(T("common.all"));
        foreach (var c in KnowledgeCategories.All) _category.Items.Add(c);
        _category.SelectedIndex = 0;
        _category.SelectedIndexChanged += async (_, _) => await FilterAsync();
        _list.DoubleClick += (_, _) =>
        {
            if (_list.SelectedItem is Application.Dtos.NoteDto n)
                Navigation.Navigate("note-editor", n.Id);
        };
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_category);
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

    private void ApplyFilter(IReadOnlyList<Application.Dtos.NoteDto> notes)
    {
        var cat = _category.SelectedItem?.ToString();
        var filtered = cat is null or "All" or "همه"
            ? notes
            : notes.Where(n => n.KnowledgeCategory == cat).ToList();
        _list.DataSource = filtered;
        _list.DisplayMember = "Title";
    }
}
