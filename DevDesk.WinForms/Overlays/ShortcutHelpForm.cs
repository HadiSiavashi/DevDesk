using DevDesk.Application.Interfaces;
using DevDesk.WinForms.Localization;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Overlays;

public sealed class ShortcutHelpForm : Form
{
    public ShortcutHelpForm()
    {
        var loc = LocalizationService.Instance;
        Text = loc.Get("shortcuts.title");
        Size = new Size(420, 360);
        StartPosition = FormStartPosition.CenterParent;
        var text = $"""
            {loc.Get("shortcuts.globalSearch")}: Ctrl+K
            {loc.Get("shortcuts.quickAdd")}: Ctrl+Shift+Space
            {loc.Get("shortcuts.newTask")}: Ctrl+N
            {loc.Get("shortcuts.save")}: Ctrl+S
            {loc.Get("shortcuts.settings")}: Ctrl+,
            Start Focus: Ctrl+Shift+F
            {loc.Get("shortcuts.help")}: F1
            Esc: Close overlay
            Space: Complete task (Tasks view)
            """;
        Controls.Add(new TextBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None
        });
        ThemeManager.Instance.ApplyTo(this);
    }
}
