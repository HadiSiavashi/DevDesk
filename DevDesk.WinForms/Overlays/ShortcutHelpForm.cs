using DevDesk.WinForms.Controls;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Overlays;

public sealed class ShortcutHelpForm : Form
{
    public ShortcutHelpForm()
    {
        var loc = LocalizationService.Instance;
        Text = loc.Get("shortcuts.title");
        Size = new Size(440, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Padding = new Padding(16);
        var rows = new (string Keys, string Label)[]
        {
            ("Ctrl+K", loc.Get("shortcuts.globalSearch")),
            ("Ctrl+Shift+Space", loc.Get("shortcuts.quickAdd")),
            ("Ctrl+N", loc.Get("shortcuts.newTask")),
            ("Ctrl+S", loc.Get("shortcuts.save")),
            ("Ctrl+,", loc.Get("shortcuts.settings")),
            ("Ctrl+Shift+F", "Start Focus"),
            ("F1", loc.Get("shortcuts.help")),
            ("Esc", "Close overlay"),
            ("Space", "Complete task (Tasks view)")
        };
        var y = 8;
        foreach (var (keys, label) in rows)
        {
            var kbd = new Label
            {
                Text = keys,
                Left = 8,
                Top = y,
                Width = 150,
                Height = 24,
                Font = UiMetrics.Kbd,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var desc = new Label
            {
                Text = label,
                Left = 168,
                Top = y,
                Width = 240,
                Height = 24,
                Font = UiMetrics.Body
            };
            Controls.Add(kbd);
            Controls.Add(desc);
            y += 32;
        }
        ThemeManager.Instance.ApplyTo(this);
        BackColor = ThemeManager.Instance.Current.Overlay;
        foreach (Control c in Controls)
        {
            if (c is Label lbl)
                lbl.ForeColor = lbl.Font == UiMetrics.Kbd
                    ? ThemeManager.Instance.Current.TextMuted
                    : ThemeManager.Instance.Current.TextPrimary;
        }
    }
}
