using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class NoteEditorView : ViewBase, ISaveableView
{
    private Guid _noteId;
    private Guid? _projectId;
    private bool _isPinned;
    private bool _isKnowledgeBase;
    private readonly TextBox _title = new() { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill };
    private readonly TextBox _content = new() { Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical };
    private readonly CheckBox _pinned = new() { Text = "Pin note", Dock = DockStyle.Top, Height = 28 };
    private readonly CheckBox _knowledge = new() { Text = "Knowledge base", Dock = DockStyle.Top, Height = 28 };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = UiMetrics.StatusBarHeight, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ModernButton _save = new() { Text = "Save", Width = 88 };
    private readonly ModernButton _back = new() { Variant = ButtonVariant.Ghost, Width = 72 };

    public NoteEditorView(IServiceScopeFactory scopeFactory, NavigationService navigation, object? parameter)
        : base(scopeFactory, navigation)
    {
        ContentPanel.Padding = new Padding(0);
        _noteId = parameter is Guid g ? g : Guid.Empty;
        _save.Click += async (_, _) => await SaveAsync();
        _back.Text = T("common.back");
        _back.Click += (_, _) => Navigation.NavigateBack();
        _pinned.CheckedChanged += (_, _) => _isPinned = _pinned.Checked;
        _knowledge.CheckedChanged += (_, _) => _isKnowledgeBase = _knowledge.Checked;

        var header = new Panel { Dock = DockStyle.Top, Height = 56, Tag = "no-theme", Padding = new Padding(UiMetrics.Space16, 8, UiMetrics.Space16, 8) };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        actions.Controls.Add(_back);
        actions.Controls.Add(_save);
        header.Controls.Add(_title);
        header.Controls.Add(actions);

        var rail = new CardPanel { Dock = DockStyle.Right, Width = 280 };
        rail.Controls.Add(_knowledge);
        rail.Controls.Add(_pinned);
        rail.Controls.Add(new Label { Text = "Properties", Dock = DockStyle.Top, Height = 24, Font = UiMetrics.SectionTitle });

        var canvas = new Panel { Dock = DockStyle.Fill, Padding = new Padding(32, 16, 32, 16), Tag = "no-theme" };
        canvas.Controls.Add(_content);

        ContentPanel.Controls.Add(canvas);
        ContentPanel.Controls.Add(rail);
        ContentPanel.Controls.Add(_status);
        ContentPanel.Controls.Add(header);
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
            _knowledge.Checked = note.IsKnowledgeBase;
            _status.Text = $"Updated {note.UpdatedAt:g}";
            ApplyEditorTheme();
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ApplyEditorTheme()
    {
        var c = ThemeManager.Instance.Current;
        _title.Font = UiMetrics.PageTitle;
        _title.ForeColor = c.TextPrimary;
        _title.BackColor = c.Background;
        _content.Font = UiMetrics.Body;
        _content.ForeColor = c.TextPrimary;
        _content.BackColor = c.Background;
        _status.Font = UiMetrics.Meta;
        _status.ForeColor = c.TextMuted;
        _status.BackColor = c.Surface;
    }

    protected override void OnThemeChanged()
    {
        base.OnThemeChanged();
        ApplyEditorTheme();
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
        _status.Text = $"Saved {DateTime.Now:g}";
    }
}
