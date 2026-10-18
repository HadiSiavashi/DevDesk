using System.Diagnostics;
using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Services;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Overlays;

public sealed class GlobalSearchForm : Form
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NavigationService _navigation;
    private readonly SearchBox _input = new() { Dock = DockStyle.Top, Height = 42, Hint = "↵" };
    private readonly ListBox _results = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false,
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 52
    };
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 200 };
    private string _pending = "";
    private List<SearchResultDto> _items = [];

    public GlobalSearchForm(IServiceScopeFactory scopeFactory, NavigationService navigation)
    {
        _scopeFactory = scopeFactory;
        _navigation = navigation;
        Text = LocalizationService.Instance.Get("search.placeholder");
        Size = new Size(680, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;
        Padding = new Padding(14);

        _input.Placeholder = LocalizationService.Instance.Get("search.placeholder");
        _debounce.Tick += async (_, _) => { _debounce.Stop(); await SearchAsync(_pending); };
        _input.TextChangedDebounced += (_, q) => { _pending = q; _debounce.Stop(); _debounce.Start(); };
        _input.Inner.KeyDown += OnInputKeyDown;

        _results.DrawItem += OnResultsDrawItem;
        _results.DoubleClick += (_, _) => NavigateSelected();
        _results.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { NavigateSelected(); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.C) { CopySelected(); e.Handled = true; }
        };
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Close();
            else if (e.Control && e.KeyCode == Keys.C) CopySelected();
        };

        var separator = new Panel { Dock = DockStyle.Top, Height = 10 };

        Controls.Add(_results);
        Controls.Add(separator);
        Controls.Add(_input);

        ThemeManager.Instance.ApplyTo(this);
        BackColor = ThemeManager.Instance.Current.Overlay;
        _results.BackColor = ThemeManager.Instance.Current.Overlay;
        _results.ForeColor = ThemeManager.Instance.Current.TextPrimary;
        _results.Font = UiMetrics.Body;

        if (LocalizationService.Instance.IsRtl)
            RightToLeft = RightToLeft.Yes;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && _items.Count > 0)
        {
            var next = Math.Min(_items.Count - 1, _results.SelectedIndex + 1);
            _results.SelectedIndex = next;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Up && _items.Count > 0)
        {
            var prev = Math.Max(0, _results.SelectedIndex - 1);
            _results.SelectedIndex = prev;
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
        _results.BeginUpdate();
        _results.Items.Clear();
        foreach (var r in _items)
            _results.Items.Add(r);
        if (_items.Count > 0)
            _results.SelectedIndex = 0;
        _results.EndUpdate();
    }

    private void OnResultsDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _items.Count) return;
        var r = _items[e.Index];
        var isSelected = (e.State & DrawItemState.Selected) != 0;
        var isRtl = LocalizationService.Instance.IsRtl;
        var c = ThemeManager.Instance.Current;
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Background
        var bounds = e.Bounds;
        using (var bgBrush = new SolidBrush(isSelected ? c.Surface : c.Overlay))
            g.FillRectangle(bgBrush, bounds);

        if (isSelected)
        {
            using var borderPen = new Pen(c.Accent, 1f);
            var rect = new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
            DrawingUtil.DrawRounded(g, borderPen, rect, 4);
        }

        // Determine Badge Color and Text
        var entity = r.EntityType.ToUpperInvariant();
        Color badgeBg;
        Color badgeFg;
        switch (entity)
        {
            case "TASK":
                badgeBg = Color.FromArgb(59, 130, 246);
                badgeFg = Color.White;
                break;
            case "PROJECT":
                badgeBg = c.Accent;
                badgeFg = Color.White;
                break;
            case "NOTE":
                badgeBg = c.Warning;
                badgeFg = Color.Black;
                break;
            case "SNIPPET":
                badgeBg = c.Success;
                badgeFg = Color.White;
                break;
            case "BOOKMARK":
                badgeBg = Color.FromArgb(14, 165, 233);
                badgeFg = Color.White;
                break;
            case "GOAL":
                badgeBg = Color.FromArgb(249, 115, 22);
                badgeFg = Color.White;
                break;
            default:
                badgeBg = c.Surface;
                badgeFg = c.TextPrimary;
                break;
        }

        var badgeText = entity;
        var badgeFont = UiMetrics.Meta;
        var badgeSize = TextRenderer.MeasureText(badgeText, badgeFont);
        var badgeWidth = badgeSize.Width + 12;
        var badgeHeight = 20;

        int badgeX, contentX, contentWidth;
        if (isRtl)
        {
            badgeX = bounds.Right - badgeWidth - 12;
            contentX = bounds.Left + 70;
            contentWidth = badgeX - contentX - 10;
        }
        else
        {
            badgeX = bounds.Left + 12;
            contentX = badgeX + badgeWidth + 12;
            contentWidth = bounds.Right - contentX - 70;
        }

        var badgeY = bounds.Top + (bounds.Height - badgeHeight) / 2;
        var badgeRect = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);

        // Draw badge pill
        using (var bBrush = new SolidBrush(badgeBg))
            DrawingUtil.FillRounded(g, bBrush, badgeRect, 4);

        TextRenderer.DrawText(g, badgeText, badgeFont, badgeRect, badgeFg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        // Title and Subtitle
        var titleRect = new Rectangle(contentX, bounds.Top + 7, contentWidth, 20);
        var subRect = new Rectangle(contentX, bounds.Top + 27, contentWidth, 18);

        var flags = isRtl ? (TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis)
                          : (TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, r.Title, UiMetrics.TaskTitle, titleRect, c.TextPrimary, flags);

        var subText = r.Subtitle ?? "";
        if (!string.IsNullOrWhiteSpace(r.ProjectName))
            subText = $"[{r.ProjectName}] {subText}";

        TextRenderer.DrawText(g, subText, UiMetrics.Meta, subRect, c.TextSecondary, flags);

        // Shortcut hint on right (or left for RTL)
        var hint = isSelected ? "↵ Open" : "";
        if (isSelected && (entity == "BOOKMARK" || entity == "SNIPPET"))
            hint = "↵ Open · Ctrl+C";

        var hintRect = isRtl ? new Rectangle(bounds.Left + 6, bounds.Top + 16, 60, 20)
                             : new Rectangle(bounds.Right - 76, bounds.Top + 16, 70, 20);

        var hintFlags = isRtl ? TextFormatFlags.Left : TextFormatFlags.Right;
        TextRenderer.DrawText(g, hint, UiMetrics.Meta, hintRect, c.TextMuted, hintFlags | TextFormatFlags.VerticalCenter);
    }

    private void CopySelected()
    {
        var idx = _results.SelectedIndex;
        if (idx < 0 || idx >= _items.Count) return;
        var r = _items[idx];
        var textToCopy = !string.IsNullOrWhiteSpace(r.Subtitle) ? r.Subtitle : r.Title;
        Clipboard.SetText(textToCopy);
    }

    private void NavigateSelected()
    {
        var idx = _results.SelectedIndex;
        if (idx < 0 || idx >= _items.Count) return;
        var r = _items[idx];

        if (string.Equals(r.EntityType, "bookmark", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(r.Subtitle) &&
            (r.Subtitle.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             r.Subtitle.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                Process.Start(new ProcessStartInfo(r.Subtitle) { UseShellExecute = true });
                Close();
                return;
            }
            catch { /* fallback to bookmarks view */ }
        }

        var key = r.EntityType.ToLowerInvariant() switch
        {
            "task" => "task-detail",
            "project" => "project-detail",
            "note" => "note-editor",
            "snippet" => "snippet-editor",
            "goal" => "goals",
            "bookmark" => "bookmarks",
            _ => "dashboard"
        };
        _navigation.Navigate(key, r.Id);
        Close();
    }
}
