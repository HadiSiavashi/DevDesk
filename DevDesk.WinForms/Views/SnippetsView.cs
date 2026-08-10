using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class SnippetsView : ViewBase
{
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _toolbar = new() { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight };
    private readonly ModernButton _add = new() { Height = 36, Text = "Add Snippet" };
    private readonly ModernButton _delete = new() { Height = 36, IsPrimary = false };

    public SnippetsView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _delete.Text = T("common.delete");
        _add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), "Title:");
            if (string.IsNullOrWhiteSpace(title)) return;
            using var scope = ScopeFactory.CreateScope();
            var s = await GetService<ISnippetService>(scope).CreateAsync(new Application.Dtos.CreateSnippetRequest { Title = title, Code = "// code" });
            Navigation.Navigate("snippet-editor", s.Id);
        };
        _delete.Click += async (_, _) => await DeleteSelectedAsync();
        _toolbar.Controls.AddRange([_add, _delete]);
        _list.DoubleClick += (_, _) =>
        {
            if (_list.SelectedItem is Application.Dtos.CodeSnippetDto s)
                Navigation.Navigate("snippet-editor", s.Id);
        };
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_toolbar);
    }

    private async Task DeleteSelectedAsync()
    {
        if (_list.SelectedItem is not Application.Dtos.CodeSnippetDto snippet) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<ISnippetService>(scope).DeleteAsync(snippet.Id);
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var snippets = await GetService<ISnippetService>(scope).GetAllAsync();
            _list.DataSource = snippets.ToList();
            _list.DisplayMember = "Title";
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}

public sealed class SnippetEditorView : ViewBase, ISaveableView
{
    private Guid _id;
    private readonly TextBox _title = new() { Dock = DockStyle.Top };
    private readonly TextBox _code = new() { Dock = DockStyle.Fill, Multiline = true, Font = new Font("Consolas", 10F), ScrollBars = ScrollBars.Both };
    private readonly FlowLayoutPanel _actions = new() { Dock = DockStyle.Bottom, Height = 40 };

    public SnippetEditorView(IServiceScopeFactory scopeFactory, NavigationService navigation, object? parameter)
        : base(scopeFactory, navigation)
    {
        _id = parameter is Guid g ? g : Guid.Empty;
        var save = new ModernButton { Text = T("common.save") };
        save.Click += async (_, _) => await SaveAsync();
        var copy = new ModernButton { Text = T("common.copy"), IsPrimary = false };
        copy.Click += (_, _) =>
        {
            if (!ClipboardHelper.TrySetText(_code.Text))
                MessageBox.Show(T("common.error"), T("common.copy"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        _actions.Controls.AddRange([save, copy]);
        ContentPanel.Controls.Add(_code);
        ContentPanel.Controls.Add(_actions);
        ContentPanel.Controls.Add(_title);
    }

    protected override async Task LoadAsync()
    {
        if (_id == Guid.Empty) return;
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var s = await GetService<ISnippetService>(scope).GetByIdAsync(_id);
            if (s is null) { ShowEmpty(); return; }
            _title.Text = s.Title;
            _code.Text = s.Code;
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }

    public async Task SaveAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var cur = await GetService<ISnippetService>(scope).GetByIdAsync(_id);
        if (cur is null) return;
        await GetService<ISnippetService>(scope).UpdateAsync(_id, new Application.Dtos.UpdateSnippetRequest
        {
            Title = _title.Text,
            Code = _code.Text,
            Language = cur.Language,
            IsFavorite = cur.IsFavorite
        });
    }
}
