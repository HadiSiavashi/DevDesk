using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class TagBadge : Label
{
    public TagBadge()
    {
        AutoSize = true;
        Padding = new Padding(6, 2, 6, 2);
        Font = new Font("Segoe UI", 7.5F);
        ForeColor = Color.White;
    }

    public void SetTag(string name, string colorHex)
    {
        Text = name;
        try { BackColor = ColorTranslator.FromHtml(colorHex); }
        catch { BackColor = ThemeManager.Instance.Current.Accent; }
    }
}
