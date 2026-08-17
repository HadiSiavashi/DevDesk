using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class BookmarksView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private BookmarkDto? _selected;

    public BookmarksView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        var add = new ModernButton { Text = T("common.add"), AutoFit = true };
        add.FitToContents();
        add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), "Title:");
            var url = Dialogs.InputDialog.Show(T("common.create"), "URL:");
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IBookmarkService>(scope).CreateAsync(new CreateBookmarkRequest { Title = title, Url = url });
            await LoadAsync();
        };
        var open = new ModernButton { Text = T("common.open"), Variant = ButtonVariant.Outline };
        open.Click += (_, _) => OpenSelectedUrl();
        var copy = new ModernButton { Text = T("common.copy"), Variant = ButtonVariant.Ghost };
        copy.Click += (_, _) =>
        {
            if (_selected is not null)
                ClipboardHelper.TrySetText(_selected.Url);
        };
        var fav = new ModernButton { Text = "★", Variant = ButtonVariant.Ghost, Width = 36 };
        fav.Click += async (_, _) => await ToggleFavoriteAsync();
        var del = new ModernButton { Text = T("common.delete"), Variant = ButtonVariant.Ghost };
        del.Click += async (_, _) => await DeleteSelectedAsync();
        var header = new PageHeader { TitleText = T("nav.bookmarks") };
        header.Actions.Controls.AddRange([add, open, copy, fav, del]);
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(header);
    }

    private void OpenSelectedUrl()
    {
        if (_selected is null) return;
        using var scope = ScopeFactory.CreateScope();
        GetService<IBrowserService>(scope).OpenUrl(_selected.Url);
    }

    private async Task ToggleFavoriteAsync()
    {
        if (_selected is null) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IBookmarkService>(scope).UpdateAsync(_selected.Id, new UpdateBookmarkRequest
        {
            Title = _selected.Title,
            Url = _selected.Url,
            Category = _selected.Category,
            IsFavorite = !_selected.IsFavorite
        });
        await LoadAsync();
    }

    private async Task DeleteSelectedAsync()
    {
        if (_selected is null) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IBookmarkService>(scope).DeleteAsync(_selected.Id);
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var items = await GetService<IBookmarkService>(scope).GetAllAsync();
            _list.Controls.Clear();
            _selected = null;
            foreach (var b in items.OrderByDescending(x => x.IsFavorite))
            {
                var row = new InventoryRow { Width = Math.Max(280, _list.ClientSize.Width - 8), Margin = new Padding(0, 0, 0, 8) };
                row.Bind(b.Title, (b.IsFavorite ? "★ " : "") + b.Url);
                row.Activated += (_, _) => { _selected = b; OpenSelectedUrl(); };
                row.Click += (_, _) => _selected = b;
                _list.Controls.Add(row);
            }
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
