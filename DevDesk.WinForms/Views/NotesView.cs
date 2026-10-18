using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Common;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class NotesView : ViewBase
{
    private readonly SegmentedTabs _tabs = new() { Dock = DockStyle.Top, UnderlineStyle = true };
    private readonly ThemedComboBox _category = new() { Dock = DockStyle.Top, Visible = false };
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ModernButton _add = new() { Text = "New Note", AutoFit = true };
    private readonly ModernButton _delete = new() { IsPrimary = false, AutoFit = true };
    private NoteDto? _selected;
    private int _tabIndex;

    public NotesView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        _delete.Text = T("common.delete");
        _add.FitToContents();
        _delete.FitToContents();

        _tabs.Items = [T("common.all"), T("nav.knowledge"), "Pinned"];
        _category.Items.Add(T("common.all"));
        foreach (var c in KnowledgeCategories.All) _category.Items.Add(c);
        _category.SelectedIndex = 0;
        _category.SelectedIndexChanged += async (_, _) => await LoadAsync();

        _tabs.SelectedIndexChanged += async (_, i) =>
        {
            _tabIndex = i;
            _category.Visible = i == 1;
            await LoadAsync();
        };

        _add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), "Title:");
            if (string.IsNullOrWhiteSpace(title)) return;
            using var scope = ScopeFactory.CreateScope();
            var isKb = _tabIndex == 1;
            var cat = isKb && _category.SelectedIndex > 0 ? _category.SelectedItem?.ToString() : null;
            var note = await GetService<INoteService>(scope).CreateAsync(new CreateNoteRequest
            {
                Title = title,
                IsKnowledgeBase = isKb,
                KnowledgeCategory = cat
            });
            Navigation.Navigate("note-editor", note.Id);
        };
        _delete.Click += async (_, _) => await DeleteSelectedAsync();
        var header = new PageHeader { TitleText = T("nav.notes") };
        header.Actions.Controls.Add(_add);
        header.Actions.Controls.Add(_delete);

        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_category);
        ContentPanel.Controls.Add(_tabs);
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

            var filtered = _tabIndex switch
            {
                1 => notes.Where(n => n.IsKnowledgeBase &&
                     (_category.SelectedIndex <= 0 || n.KnowledgeCategory == _category.SelectedItem?.ToString())).ToList(),
                2 => notes.Where(n => n.IsPinned).ToList(),
                _ => notes
            };

            _list.Controls.Clear();
            _selected = null;
            foreach (var n in filtered)
            {
                var row = new InventoryRow { Width = Math.Max(280, _list.ClientSize.Width - 8), Margin = new Padding(0, 0, 0, 8) };
                row.Item = n;
                var subtitle = n.IsKnowledgeBase && !string.IsNullOrEmpty(n.KnowledgeCategory)
                    ? $"[{n.KnowledgeCategory}] · {(n.IsPinned ? "Pinned · " : "")}{n.UpdatedAt:MMM d}"
                    : (n.IsPinned ? "Pinned · " : "") + n.UpdatedAt.ToString("MMM d");
                row.Bind(n.Title, subtitle);
                row.Activated += (_, _) =>
                {
                    _selected = n;
                    Navigation.Navigate("note-editor", n.Id);
                };
                row.Click += (_, _) => _selected = n;
                _list.Controls.Add(row);
            }

            if (filtered.Count == 0)
            {
                var isRtl = Localization.LocalizationService.Instance.IsRtl;
                var empty = new EmptyStatePanel
                {
                    Dock = DockStyle.Fill,
                    Message = isRtl
                        ? "هیچ یادداشتی در این بخش وجود ندارد.\r\nروی «افزودن» در بالا کلیک کنید."
                        : "No notes found in this section.\r\nClick '+ Add' above to create a new note."
                };
                _list.Controls.Add(empty);
            }
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
