using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public class IconButton : Button
{
    public IconButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Size = new Size(36, 36);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 10F);
        ThemeManager.Instance.ThemeChanged += (_, _) => Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var c = ThemeManager.Instance.Current;
        using var brush = new SolidBrush(BackColor == Color.Empty ? c.Surface : BackColor);
        pevent.Graphics.FillRectangle(brush, ClientRectangle);
        TextRenderer.DrawText(pevent.Graphics, Text, Font, ClientRectangle, c.TextPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}
