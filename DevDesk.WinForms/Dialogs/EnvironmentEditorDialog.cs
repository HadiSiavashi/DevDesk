using DevDesk.Application.Dtos;
using DevDesk.Application.Interfaces;
using DevDesk.Domain.Enums;
using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Dialogs;

public sealed class EnvironmentEditorDialog : ModalForm
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Guid _projectId;
    private readonly EnvironmentDto? _existing;

    private readonly TextField _name = new();
    private readonly ThemedComboBox _type = new();
    private readonly TextField _baseUrl = new();
    private readonly TextField _dbServer = new();
    private readonly TextField _dbName = new();
    private readonly TextField _notes = new();
    private readonly Label _error = new() { AutoSize = false, Height = 28, Visible = false };
    private readonly ModernButton _save = new();
    private readonly ModernButton _cancel = new() { IsPrimary = false };

    public EnvironmentDto? Result { get; private set; }

    public static bool ShowCreate(IWin32Window owner, IServiceScopeFactory scopeFactory, Guid projectId)
    {
        using var dlg = new EnvironmentEditorDialog(scopeFactory, projectId, null);
        return dlg.ShowDialog(owner) == DialogResult.OK;
    }

    public static bool ShowEdit(IWin32Window owner, IServiceScopeFactory scopeFactory, EnvironmentDto existing)
    {
        using var dlg = new EnvironmentEditorDialog(scopeFactory, existing.ProjectId, existing);
        return dlg.ShowDialog(owner) == DialogResult.OK;
    }

    private EnvironmentEditorDialog(IServiceScopeFactory scopeFactory, Guid projectId, EnvironmentDto? existing)
    {
        _scopeFactory = scopeFactory;
        _projectId = projectId;
        _existing = existing;

        Text = existing is null ? "Add Environment" : "Edit Environment";
        ClientSize = new Size(500, 480);
        Padding = new Padding(UiMetrics.Space16);
        BackColor = ThemeManager.Instance.Current.Overlay;

        foreach (EnvironmentType t in Enum.GetValues(typeof(EnvironmentType)))
            _type.Items.Add(t);
        _type.SelectedItem = existing?.EnvironmentType ?? EnvironmentType.Development;

        _name.PlaceholderText = "e.g. Development, Staging, Production";
        _baseUrl.PlaceholderText = "https://localhost:5001 or https://api.myproject.com";
        _dbServer.PlaceholderText = "localhost or sql.myproject.internal";
        _dbName.PlaceholderText = "DevDesk_Dev";
        _notes.Multiline = true;
        _notes.Height = 60;
        _notes.PlaceholderText = "Additional connection details, port, or notes";

        if (existing is not null)
        {
            _name.Text = existing.Name;
            _baseUrl.Text = existing.BaseUrl ?? "";
            _dbServer.Text = existing.DatabaseServer ?? "";
            _dbName.Text = existing.DatabaseName ?? "";
            _notes.Text = existing.Notes ?? "";
        }

        _save.Text = T("common.save");
        _cancel.Text = T("common.cancel");
        _cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _save.Click += async (_, _) => await OnSaveAsync();

        var isRtl = LocalizationService.Instance.IsRtl;
        var host = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No
        };

        Control FieldRow(string label, Control input)
        {
            var p = new Panel { Width = 450, Height = input.Height + 24, Margin = new Padding(0, 0, 0, 6) };
            var lbl = new Label
            {
                Text = label,
                Font = UiMetrics.Meta,
                ForeColor = ThemeManager.Instance.Current.TextSecondary,
                Dock = DockStyle.Top,
                Height = 18
            };
            input.Dock = DockStyle.Bottom;
            p.Controls.Add(input);
            p.Controls.Add(lbl);
            return p;
        }

        host.Controls.Add(FieldRow("Environment Name *", _name));
        host.Controls.Add(FieldRow("Type", _type));
        host.Controls.Add(FieldRow("Base URL", _baseUrl));
        host.Controls.Add(FieldRow("Database Server", _dbServer));
        host.Controls.Add(FieldRow("Database Name", _dbName));
        host.Controls.Add(FieldRow("Notes", _notes));

        _error.ForeColor = ThemeManager.Instance.Current.Error;
        _error.Font = UiMetrics.Meta;
        _error.Width = 450;
        host.Controls.Add(_error);

        var btnBar = new Panel { Dock = DockStyle.Bottom, Height = 42 };
        _cancel.Width = 84;
        _cancel.Height = UiMetrics.ButtonHeight;
        _save.Width = 84;
        _save.Height = UiMetrics.ButtonHeight;

        if (isRtl)
        {
            _save.Left = 0;
            _cancel.Left = 92;
        }
        else
        {
            _cancel.Left = btnBar.Width - 90;
            _save.Left = _cancel.Left - 92;
            _cancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _save.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        }

        btnBar.Controls.Add(_save);
        btnBar.Controls.Add(_cancel);

        Controls.Add(host);
        Controls.Add(btnBar);

        AcceptButton = _save;
        CancelButton = _cancel;
    }

    private async Task OnSaveAsync()
    {
        var name = _name.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _error.Text = "Please enter an environment name.";
            _error.Visible = true;
            return;
        }

        _error.Visible = false;
        _save.Enabled = false;
        _cancel.Enabled = false;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEnvironmentService>();
            var type = _type.SelectedItem is EnvironmentType t ? t : EnvironmentType.Development;

            if (_existing is null)
            {
                Result = await service.CreateAsync(new CreateEnvironmentRequest
                {
                    ProjectId = _projectId,
                    Name = name,
                    EnvironmentType = type,
                    BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.Text) ? null : _baseUrl.Text.Trim(),
                    DatabaseServer = string.IsNullOrWhiteSpace(_dbServer.Text) ? null : _dbServer.Text.Trim(),
                    DatabaseName = string.IsNullOrWhiteSpace(_dbName.Text) ? null : _dbName.Text.Trim(),
                    Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
                });
            }
            else
            {
                Result = await service.UpdateAsync(_existing.Id, new UpdateEnvironmentRequest
                {
                    Name = name,
                    EnvironmentType = type,
                    BaseUrl = string.IsNullOrWhiteSpace(_baseUrl.Text) ? null : _baseUrl.Text.Trim(),
                    DatabaseServer = string.IsNullOrWhiteSpace(_dbServer.Text) ? null : _dbServer.Text.Trim(),
                    DatabaseName = string.IsNullOrWhiteSpace(_dbName.Text) ? null : _dbName.Text.Trim(),
                    Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
                });
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _error.Text = ex.Message;
            _error.Visible = true;
            _save.Enabled = true;
            _cancel.Enabled = true;
        }
    }
}
