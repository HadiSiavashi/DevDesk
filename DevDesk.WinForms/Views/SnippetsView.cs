using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Views;

public sealed class SnippetsView : ViewBase
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ModernButton _add = new() { Height = 36, Text = "Add Snippet" };
    private readonly ModernButton _delete = new() { Height = 36, IsPrimary = false };
    private Application.Dtos.CodeSnippetDto? _selected;

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
        var header = new PageHeader { TitleText = T("nav.snippets") };
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
        await GetService<ISnippetService>(scope).DeleteAsync(_selected.Id);
        await LoadAsync();
    }

    protected override async Task LoadAsync()
    {
        ShowLoading();
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var snippets = await GetService<ISnippetService>(scope).GetAllAsync();
            _list.Controls.Clear();
            _selected = null;
            foreach (var s in snippets)
            {
                var row = new InventoryRow { Width = Math.Max(280, _list.ClientSize.Width - 8), Margin = new Padding(0, 0, 0, 8) };
                row.Bind(s.Title, s.Language);
                row.Activated += (_, _) => { _selected = s; Navigation.Navigate("snippet-editor", s.Id); };
                row.Click += (_, _) => _selected = s;
                _list.Controls.Add(row);
            }
            ShowContent();
        }
        catch (Exception ex) { ShowError(ex); }
    }
}

public sealed class SnippetEditorView : ViewBase, ISaveableView
{
    private Guid _id;
    private bool _isFavorite;
    private readonly TextBox _title = new() { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill };
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Height = 28 };
    private readonly TextBox _code = new() { Dock = DockStyle.Fill, Multiline = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Both };
    private readonly Panel _gutter = new() { Dock = DockStyle.Left, Width = 44, Tag = "no-theme" };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = UiMetrics.StatusBarHeight, TextAlign = ContentAlignment.MiddleLeft };

    public SnippetEditorView(IServiceScopeFactory scopeFactory, NavigationService navigation, object? parameter)
        : base(scopeFactory, navigation)
    {
        ContentPanel.Padding = new Padding(0);
        _id = parameter is Guid g ? g : Guid.Empty;
        _language.Items.AddRange(["C#", "TypeScript", "JavaScript", "Python", "SQL", "JSON", "HTML", "CSS", "PowerShell"]);
        _language.SelectedItem = "C#";

        var save = new ModernButton { Text = T("common.save"), Width = 88 };
        save.Click += async (_, _) => await SaveAsync();
        var copy = new ModernButton { Text = T("common.copy"), Variant = ButtonVariant.Outline, Width = 88 };
        copy.Click += (_, _) =>
        {
            if (!ClipboardHelper.TrySetText(_code.Text))
                MessageBox.Show(T("common.error"), T("common.copy"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        var back = new ModernButton { Text = T("common.back"), Variant = ButtonVariant.Ghost, Width = 72 };
        back.Click += (_, _) => Navigation.NavigateBack();

        var header = new Panel { Dock = DockStyle.Top, Height = 56, Tag = "no-theme", Padding = new Padding(UiMetrics.Space16, 8, UiMetrics.Space16, 8) };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        actions.Controls.Add(_language);
        actions.Controls.Add(copy);
        actions.Controls.Add(save);
        actions.Controls.Add(back);
        header.Controls.Add(_title);
        header.Controls.Add(actions);

        _gutter.Paint += PaintGutter;
        _code.TextChanged += (_, _) => _gutter.Invalidate();
        _code.MouseWheel += (_, _) => _gutter.Invalidate();

        var canvas = new Panel { Dock = DockStyle.Fill, Tag = "no-theme" };
        canvas.Controls.Add(_code);
        canvas.Controls.Add(_gutter);

        ContentPanel.Controls.Add(canvas);
        ContentPanel.Controls.Add(_status);
        ContentPanel.Controls.Add(header);
    }

    private void PaintGutter(object? sender, PaintEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        e.Graphics.Clear(c.SurfaceAlt);
        var lines = Math.Max(1, _code.GetLineFromCharIndex(_code.TextLength) + 1);
        for (var i = 1; i <= lines; i++)
        {
            var y = (i - 1) * _code.Font.Height + 4;
            TextRenderer.DrawText(e.Graphics, i.ToString(), UiMetrics.Mono, new Rectangle(0, y, _gutter.Width - 6, _code.Font.Height),
                c.TextMuted, TextFormatFlags.Right);
        }
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
            _isFavorite = s.IsFavorite;
            if (!string.IsNullOrWhiteSpace(s.Language))
            {
                if (!_language.Items.Contains(s.Language))
                    _language.Items.Add(s.Language);
                _language.SelectedItem = s.Language;
            }
            _status.Text = s.Language;
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
        _code.Font = UiMetrics.Mono;
        _code.ForeColor = c.TextPrimary;
        _code.BackColor = c.Overlay;
        _status.Font = UiMetrics.Meta;
        _status.ForeColor = c.TextMuted;
        _status.BackColor = c.Surface;
        _gutter.Invalidate();
    }

    protected override void OnThemeChanged()
    {
        base.OnThemeChanged();
        ApplyEditorTheme();
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
            Language = _language.SelectedItem?.ToString() ?? cur.Language,
            IsFavorite = _isFavorite
        });
        _status.Text = $"Saved {DateTime.Now:g}";
    }
}
