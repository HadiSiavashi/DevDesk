using DevDesk.Application.Interfaces;
using DevDesk.Application.Parsing;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Overlays;

public sealed class QuickAddForm : Form
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TextBox _input = new() { Dock = DockStyle.Top, Height = 32 };
    private readonly FlowLayoutPanel _chips = new()
    {
        Dock = DockStyle.Top,
        Height = 36,
        AutoSize = false,
        WrapContents = false,
        Padding = new Padding(0, 4, 0, 4)
    };
    private readonly Label _preview = new()
    {
        Dock = DockStyle.Fill,
        Text = LocalizationService.Instance.Get("quickadd.preview"),
        Font = new Font("Segoe UI Semibold", 11F),
        TextAlign = ContentAlignment.TopLeft,
        Padding = new Padding(4)
    };

    public QuickAddForm(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Text = LocalizationService.Instance.Get("quickadd.placeholder");
        Size = new Size(480, 220);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        _input.PlaceholderText = LocalizationService.Instance.Get("quickadd.placeholder");
        _input.TextChanged += (_, _) => UpdatePreview();
        _input.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { await CreateAsync(); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) { Close(); e.Handled = true; }
        };
        var create = new Button { Text = LocalizationService.Instance.Get("common.create"), Dock = DockStyle.Bottom, Height = 36 };
        create.Click += async (_, _) => await CreateAsync();
        Controls.Add(_preview);
        Controls.Add(_chips);
        Controls.Add(create);
        Controls.Add(_input);
        ThemeManager.Instance.ApplyTo(this);
    }

    private void UpdatePreview()
    {
        _chips.Controls.Clear();
        if (string.IsNullOrWhiteSpace(_input.Text))
        {
            _preview.Text = LocalizationService.Instance.Get("quickadd.preview");
            return;
        }

        try
        {
            var parsed = QuickAddParser.Parse(_input.Text, DateOnly.FromDateTime(DateTime.Today));
            _preview.Text = parsed.Title;
            AddChip(_chips, "project", parsed.ProjectName);
            AddChip(_chips, "priority", parsed.Priority?.ToString());
            AddChip(_chips, "due", parsed.DueDate?.ToString("MMM d"));
            AddChip(_chips, "estimate", parsed.EstimatedMinutes is int m ? $"{m}m" : null);
        }
        catch (FormatException ex)
        {
            _preview.Text = ex.Message;
        }
        catch (ArgumentException ex)
        {
            _preview.Text = ex.Message;
        }
    }

    private static void AddChip(FlowLayoutPanel panel, string kind, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var c = ThemeManager.Instance.Current;
        panel.Controls.Add(new Label
        {
            Text = $"{kind}: {value}",
            AutoSize = true,
            Margin = new Padding(0, 0, 6, 0),
            Padding = new Padding(6, 2, 6, 2),
            BackColor = c.SurfaceAlt,
            ForeColor = c.TextSecondary,
            Font = new Font("Segoe UI", 8F)
        });
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(_input.Text)) return;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ITaskService>().CreateFromQuickAddAsync(_input.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _preview.Text = ex.Message;
        }
    }
}
