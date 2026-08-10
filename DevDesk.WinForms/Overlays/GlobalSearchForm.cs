using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Overlays;

public sealed class GlobalSearchForm : Form
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NavigationService _navigation;
    private readonly TextBox _input = new() { Dock = DockStyle.Top, Height = 36, Font = new Font("Segoe UI", 11F) };
    private readonly ListBox _results = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 250 };
    private string _pending = "";
    private List<SearchResultDto> _items = [];

    public GlobalSearchForm(IServiceScopeFactory scopeFactory, NavigationService navigation)
    {
        _scopeFactory = scopeFactory;
        _navigation = navigation;
        Text = LocalizationService.Instance.Get("search.placeholder");
        Size = new Size(640, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;

        _input.PlaceholderText = LocalizationService.Instance.Get("search.placeholder");
        _debounce.Tick += async (_, _) => { _debounce.Stop(); await SearchAsync(_pending); };
        _input.TextChanged += (_, _) => { _pending = _input.Text; _debounce.Stop(); _debounce.Start(); };
        _input.KeyDown += OnInputKeyDown;
        _results.DoubleClick += (_, _) => NavigateSelected();
        _results.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { NavigateSelected(); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
        };
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        Controls.Add(_results);
        Controls.Add(_input);
        ThemeManager.Instance.ApplyTo(this);
        BackColor = ThemeManager.Instance.Current.Background;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && _results.Items.Count > 0)
        {
            _results.Focus();
            _results.SelectedIndex = 0;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            if (_results.SelectedIndex >= 0)
                NavigateSelected();
            else if (_items.Count > 0)
            {
                _results.SelectedIndex = 0;
                NavigateSelected();
            }
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private async Task SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _items = [];
            _results.Items.Clear();
            return;
        }
        using var scope = _scopeFactory.CreateScope();
        var results = await scope.ServiceProvider.GetRequiredService<ISearchService>().SearchAsync(query);
        _items = results.ToList();
        _results.Items.Clear();
        foreach (var r in _items)
            _results.Items.Add(FormatResult(r));
    }

    private static string FormatResult(SearchResultDto r)
    {
        var prefix = string.IsNullOrWhiteSpace(r.EntityType) ? "" : $"[{r.EntityType}] ";
        var sub = string.IsNullOrWhiteSpace(r.Subtitle) ? "" : $" — {r.Subtitle}";
        return $"{prefix}{r.Title}{sub}";
    }

    private void NavigateSelected()
    {
        var idx = _results.SelectedIndex;
        if (idx < 0 || idx >= _items.Count) return;
        var r = _items[idx];
        var key = r.EntityType.ToLowerInvariant() switch
        {
            "task" => "task-detail",
            "project" => "project-detail",
            "note" => "note-editor",
            "snippet" => "snippet-editor",
            _ => "dashboard"
        };
        _navigation.Navigate(key, r.Id);
        Close();
    }
}
