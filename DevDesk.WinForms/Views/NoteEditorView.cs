using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class NoteEditorView : ViewBase, ISaveableView
{
    private Guid _noteId;
    private Guid? _projectId;
    private bool _isPinned;
    private bool _isKnowledgeBase;
    private readonly TextBox _title = new() { Dock = DockStyle.Top };
    private readonly TextBox _content = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _pinned = new() { Text = "Pin note", Dock = DockStyle.Top, Height = 24 };
    private readonly ModernButton _save = new() { Dock = DockStyle.Bottom, Height = 36, Text = "Save" };

    public NoteEditorView(IServiceScopeFactory scopeFactory, NavigationService navigation, object? parameter)
        : base(scopeFactory, navigation)
    {
        _noteId = parameter is Guid g ? g : Guid.Empty;
        _save.Click += async (_, _) => await SaveAsync();
        _pinned.CheckedChanged += (_, _) => _isPinned = _pinned.Checked;
        ContentPanel.Controls.Add(_content);
        ContentPanel.Controls.Add(_save);
        ContentPanel.Controls.Add(_pinned);
        ContentPanel.Controls.Add(_title);
    }

    protected override async Task LoadAsync()
    {
        if (_noteId == Guid.Empty) return;
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var note = await GetService<INoteService>(scope).GetByIdAsync(_noteId);
            if (note is null) { ShowEmpty(); return; }
            _title.Text = note.Title;
            _content.Text = note.Content;
            _projectId = note.ProjectId;
            _isPinned = note.IsPinned;
            _isKnowledgeBase = note.IsKnowledgeBase;
            _pinned.Checked = note.IsPinned;
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    public async Task SaveAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        await GetService<INoteService>(scope).UpdateAsync(_noteId, new Application.Dtos.UpdateNoteRequest
        {
            Title = _title.Text,
            Content = _content.Text,
            ProjectId = _projectId,
            IsKnowledgeBase = _isKnowledgeBase,
            IsPinned = _isPinned
        });
    }
}
