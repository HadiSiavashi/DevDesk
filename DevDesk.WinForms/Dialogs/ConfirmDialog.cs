using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Dialogs;

public static class ConfirmDialog
{
    public static bool Show(string title, string message)
        => ShowDetailed(title, message, null, null);

    public static bool ShowDetailed(string title, string message, string? primaryDetail, string? secondaryDetail)
    {
        using var form = new ModalForm
        {
            Text = title,
            ClientSize = new Size(400, string.IsNullOrEmpty(primaryDetail) ? 160 : 220)
        };

        var y = UiMetrics.Space16;
        var msg = new Label
        {
            Text = message,
            Left = UiMetrics.Space16,
            Top = y,
            Width = form.ClientSize.Width - UiMetrics.Space32,
            Height = 40,
            Font = UiMetrics.Body
        };
        form.Controls.Add(msg);
        y += 48;

        if (!string.IsNullOrWhiteSpace(primaryDetail))
        {
            var a = new Label
            {
                Text = primaryDetail,
                Left = UiMetrics.Space16,
                Top = y,
                Width = form.ClientSize.Width - UiMetrics.Space32,
                Height = 36,
                Font = UiMetrics.Meta,
                ForeColor = ThemeManager.Instance.Current.TextSecondary
            };
            form.Controls.Add(a);
            y += 40;
        }

        if (!string.IsNullOrWhiteSpace(secondaryDetail))
        {
            var b = new Label
            {
                Text = secondaryDetail,
                Left = UiMetrics.Space16,
                Top = y,
                Width = form.ClientSize.Width - UiMetrics.Space32,
                Height = 36,
                Font = UiMetrics.Meta,
                ForeColor = ThemeManager.Instance.Current.TextSecondary
            };
            form.Controls.Add(b);
            y += 40;
        }

        var cancel = new ModernButton
        {
            Text = LocalizationService.Instance.Get("common.cancel"),
            IsPrimary = false,
            Width = 90,
            Height = UiMetrics.ButtonHeight,
            Top = y + UiMetrics.Space8
        };
        cancel.Left = form.ClientSize.Width - UiMetrics.Space16 - cancel.Width;
        var ok = new ModernButton
        {
            Text = LocalizationService.Instance.Get("common.confirm"),
            Width = 100,
            Height = UiMetrics.ButtonHeight,
            Top = cancel.Top
        };
        ok.Left = cancel.Left - UiMetrics.Space8 - ok.Width;
        ok.Click += (_, _) => form.DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.ClientSize = new Size(400, cancel.Bottom + UiMetrics.Space16);
        ThemeManager.Instance.ApplyTo(form);
        return form.ShowDialog() == DialogResult.OK;
    }
}

public static class InputDialog
{
    public static string? Show(string title, string prompt, string defaultValue = "")
    {
        using var form = new ModalForm
        {
            Text = title,
            ClientSize = new Size(400, 160)
        };
        var lbl = new Label { Text = prompt, Left = 16, Top = 16, Width = 360, Height = 20 };
        var txt = new TextBox { Text = defaultValue, Left = 16, Top = 40, Width = 360, Height = UiMetrics.InputHeight };
        var ok = new ModernButton { Text = LocalizationService.Instance.Get("common.ok"), Left = 200, Top = 88, Width = 80, Height = UiMetrics.ButtonHeight };
        var cancel = new ModernButton { Text = LocalizationService.Instance.Get("common.cancel"), IsPrimary = false, Left = 288, Top = 88, Width = 80, Height = UiMetrics.ButtonHeight };
        ok.Click += (_, _) => form.DialogResult = DialogResult.OK;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        form.Controls.AddRange([lbl, txt, ok, cancel]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        ThemeManager.Instance.ApplyTo(form);
        return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
    }
}
