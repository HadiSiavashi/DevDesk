using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class NotesView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ModernButton _add = new() { Height = 36, Text = "New Note" };
    private readonly ModernButton _delete = new() { Height = 36, IsPrimary = false };
    private Application.Dtos.NoteDto? _selected;

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
        var header = new PageHeader { TitleText = T("nav.notes") };
        header.Actions.Controls.Add(_add);
        header.Actions.Controls.Add(_delete);
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(header);
    }

    private async Task DeleteSelectedAsync()
    {
        if (_selected is null) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<INoteService>(scope).DeleteAsync(_selected.Id);
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var notes = await GetService<INoteService>(scope).GetAllAsync();
            _list.Controls.Clear();
            _selected = null;
            foreach (var n in notes)
            {
                var row = new InventoryRow { Width = Math.Max(280, _list.ClientSize.Width - 8), Margin = new Padding(0, 0, 0, 8) };
                row.Item = n;
                row.Bind(n.Title, n.IsPinned ? "Pinned" : n.UpdatedAt.ToString("MMM d"));
                row.Activated += (_, _) =>
                {
                    _selected = n;
                    Navigation.Navigate("note-editor", n.Id);
                };
                row.Click += (_, _) => _selected = n;
                _list.Controls.Add(row);
            }
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
