using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class NotesView : ViewBase
{
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _toolbar = new() { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight };
    private readonly ModernButton _add = new() { Height = 36, Text = "New Note" };
    private readonly ModernButton _delete = new() { Height = 36, IsPrimary = false };

    public NotesView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _delete.Text = T("common.delete");
        _add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), "Title:");
            if (string.IsNullOrWhiteSpace(title)) return;
            using var scope = ScopeFactory.CreateScope();
            var note = await GetService<INoteService>(scope).CreateAsync(new Application.Dtos.CreateNoteRequest { Title = title });
            Navigation.Navigate("note-editor", note.Id);
        };
        _delete.Click += async (_, _) => await DeleteSelectedAsync();
        _toolbar.Controls.AddRange([_add, _delete]);
        _list.DoubleClick += (_, _) =>
        {
            if (_list.SelectedItem is Application.Dtos.NoteDto n)
                Navigation.Navigate("note-editor", n.Id);
        };
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_toolbar);
    }

    private async Task DeleteSelectedAsync()
    {
        if (_list.SelectedItem is not Application.Dtos.NoteDto note) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<INoteService>(scope).DeleteAsync(note.Id);
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var notes = await GetService<INoteService>(scope).GetAllAsync();
            _list.DataSource = notes.ToList();
            _list.DisplayMember = "Title";
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
