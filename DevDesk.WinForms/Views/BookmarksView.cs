using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class BookmarksView : ViewBase
{
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _actions = new() { Dock = DockStyle.Bottom, Height = 40 };

    public BookmarksView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        var add = new ModernButton { Text = T("common.add") };
        add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), "Title:");
            var url = Dialogs.InputDialog.Show(T("common.create"), "URL:");
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(url)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IBookmarkService>(scope).CreateAsync(new Application.Dtos.CreateBookmarkRequest { Title = title, Url = url });
            await LoadAsync();
        };
        var open = new ModernButton { Text = T("common.open"), IsPrimary = false };
        open.Click += (_, _) => OpenSelectedUrl();
        var copy = new ModernButton { Text = T("common.copy"), IsPrimary = false };
        copy.Click += (_, _) =>
        {
            if (_list.SelectedItem is Application.Dtos.BookmarkDto b)
                ClipboardHelper.TrySetText(b.Url);
        };
        var fav = new ModernButton { Text = "★", IsPrimary = false };
        fav.Click += async (_, _) => await ToggleFavoriteAsync();
        var del = new ModernButton { Text = T("common.delete"), IsPrimary = false };
        del.Click += async (_, _) => await DeleteSelectedAsync();
        _actions.Controls.AddRange([add, open, copy, fav, del]);
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(_actions);
    }

    private void OpenSelectedUrl()
    {
        if (_list.SelectedItem is not Application.Dtos.BookmarkDto b) return;
        using var scope = ScopeFactory.CreateScope();
        GetService<IBrowserService>(scope).OpenUrl(b.Url);
    }

    private async Task ToggleFavoriteAsync()
    {
        if (_list.SelectedItem is not Application.Dtos.BookmarkDto b) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IBookmarkService>(scope).UpdateAsync(b.Id, new Application.Dtos.UpdateBookmarkRequest
        {
            Title = b.Title,
            Url = b.Url,
            Category = b.Category,
            IsFavorite = !b.IsFavorite
        });
        await LoadAsync();
    }

    private async Task DeleteSelectedAsync()
    {
        if (_list.SelectedItem is not Application.Dtos.BookmarkDto b) return;
        if (!Dialogs.ConfirmDialog.Show(T("common.confirm"), T("common.delete"))) return;
        using var scope = ScopeFactory.CreateScope();
        await GetService<IBookmarkService>(scope).DeleteAsync(b.Id);
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var items = await GetService<IBookmarkService>(scope).GetAllAsync();
            _list.DataSource = items.OrderByDescending(x => x.IsFavorite).ToList();
            _list.DisplayMember = "Title";
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
