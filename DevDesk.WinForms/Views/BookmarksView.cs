using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class BookmarksView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private BookmarkDto? _selected;

    public BookmarksView(IServiceScopeFactory scopeFactory, NavigationService navigation) : base(scopeFactory, navigation)
    {
        var add = new ModernButton { Text = T("bookmarks.add"), AutoFit = true };
        add.FitToContents();
        add.Click += async (_, _) =>
        {
            var title = Dialogs.InputDialog.Show(T("common.create"), $"{T("common.title")}:");
            if (string.IsNullOrWhiteSpace(title)) return;
            var url = Dialogs.InputDialog.Show(T("common.create"), $"{T("common.url")}:");
            if (string.IsNullOrWhiteSpace(url)) return;
            using var scope = ScopeFactory.CreateScope();
            await GetService<IBookmarkService>(scope).CreateAsync(new CreateBookmarkRequest { Title = title, Url = url });
            await LoadAsync();
        };

        var open = new ModernButton { Text = T("common.open"), Variant = ButtonVariant.Outline, AutoFit = true };
        open.FitToContents();
        open.Click += (_, _) => OpenSelectedUrl();

        var copy = new ModernButton { Text = T("common.copy"), Variant = ButtonVariant.Ghost, AutoFit = true };
        copy.FitToContents();
        copy.Click += (_, _) =>
        {
            if (_selected is not null)
                ClipboardHelper.TrySetText(_selected.Url);
        };

        var fav = new ModernButton { Text = "★", Variant = ButtonVariant.Ghost, Width = UiScale.Px(36) };
        fav.Click += async (_, _) => await ToggleFavoriteAsync();

        var del = new ModernButton { Text = T("common.delete"), Variant = ButtonVariant.Ghost, AutoFit = true };
        del.FitToContents();
        del.Click += async (_, _) => await DeleteSelectedAsync();

        var header = new PageHeader { TitleText = T("nav.bookmarks") };
        header.Actions.Controls.AddRange([add, open, copy, fav, del]);
        ContentPanel.Controls.Add(_list);
        ContentPanel.Controls.Add(header);

        _list.Resize += (_, _) =>
        {
            var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));
            foreach (Control c in _list.Controls)
                c.Width = targetW;
        };
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

            var ordered = items.OrderByDescending(x => x.IsFavorite).ToList();
            if (ordered.Count == 0)
            {
                var empty = new EmptyStatePanel
                {
                    TitleText = T("bookmarks.empty"),
                    Icon = "bookmark_border",
                    Width = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16)),
                    Height = UiScale.Px(200)
                };
                _list.Controls.Add(empty);
            }
            else
            {
                var targetW = Math.Max(UiScale.Px(320), _list.ClientSize.Width - UiScale.Px(16));
                foreach (var b in ordered)
                {
                    var row = new InventoryRow
                    {
                        Width = targetW,
                        Margin = new Padding(0, 0, 0, UiMetrics.Space8),
                        Item = b
                    };
                    row.Bind(b.Title, (b.IsFavorite ? "★ " : "") + b.Url);
                    row.Activated += (_, _) => { _selected = b; OpenSelectedUrl(); };
                    row.Click += (_, _) =>
                    {
                        _selected = b;
                        foreach (Control c in _list.Controls)
                        {
                            if (c is InventoryRow r)
                                r.Selected = ReferenceEquals(r.Item, b);
                        }
                    };
                    _list.Controls.Add(row);
                }
            }

            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}
