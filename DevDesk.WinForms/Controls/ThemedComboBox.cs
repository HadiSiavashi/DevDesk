using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class ThemedComboBox : ComboBox
{
    public ThemedComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        DrawMode = DrawMode.OwnerDrawFixed;
        IntegralHeight = false;
        ItemHeight = UiMetrics.ComboItemHeight;
        FlatStyle = FlatStyle.Flat;
        Height = UiMetrics.InputHeight;
        DrawItem += OnDrawItemCore;
        DropDown += (_, _) =>
        {
            if (!IsDisposed && IsHandleCreated)
                DrawingUtil.ApplyComboDropDownTheme(this);
        };
        HandleCreated += (_, _) =>
        {
            if (!IsDisposed)
                DrawingUtil.ApplyWindowChrome(this);
        };
        ThemeManager.Instance.Attach(this, (_, _) => ApplyTheme());
        UiScale.Attach(this, (_, _) => ApplyTheme());
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (IsDisposed || Disposing) return;

        var c = ThemeManager.Instance.Current;
        BackColor = c.InputBg;
        ForeColor = c.TextPrimary;
        Font = UiMetrics.Body;
        Height = UiMetrics.InputHeight;
        ItemHeight = UiMetrics.ComboItemHeight;
        if (IsHandleCreated)
        {
            Invalidate();
            DrawingUtil.ApplyWindowChrome(this);
        }
    }

    private void OnDrawItemCore(object? sender, DrawItemEventArgs e)
    {
        if (IsDisposed) return;
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
