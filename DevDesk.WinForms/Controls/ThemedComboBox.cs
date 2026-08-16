using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class ThemedComboBox : ComboBox
{
    public ThemedComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        DrawMode = DrawMode.OwnerDrawFixed;
        IntegralHeight = false;
        ItemHeight = 26;
        FlatStyle = FlatStyle.Flat;
        Height = UiMetrics.InputHeight;
        DrawItem += OnDrawItemCore;
        DropDown += (_, _) => DrawingUtil.ApplyComboDropDownTheme(this);
        HandleCreated += (_, _) => DrawingUtil.ApplyWindowChrome(this);
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var c = ThemeManager.Instance.Current;
        BackColor = c.InputBg;
        ForeColor = c.TextPrimary;
        Font = UiMetrics.Body;
        Invalidate();
        if (IsHandleCreated)
            DrawingUtil.ApplyWindowChrome(this);
    }

    private void OnDrawItemCore(object? sender, DrawItemEventArgs e)
    {
        var c = ThemeManager.Instance.Current;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(selected ? c.SelectedBg : c.InputBg);
        e.Graphics.FillRectangle(bg, e.Bounds);
        if (e.Index < 0) return;
        var text = GetItemText(Items[e.Index]);
        TextRenderer.DrawText(e.Graphics, text, UiMetrics.Body,
            Rectangle.Inflate(e.Bounds, -8, 0),
            selected ? c.TextPrimary : c.TextSecondary,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }
}
