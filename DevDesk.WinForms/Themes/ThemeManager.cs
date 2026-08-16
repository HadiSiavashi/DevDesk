using DevDesk.Domain.Enums;
using Microsoft.Win32;

namespace DevDesk.WinForms.Themes;

public sealed class ThemeManager
{
    public static ThemeManager Instance { get; } = new();

    private ThemeMode _mode = ThemeMode.System;

    public ThemeMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            Current = ResolveColors(value);
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public AppColors Current { get; private set; } = DarkTheme.Create();

    public event EventHandler? ThemeChanged;

    private ThemeManager()
    {
        Current = ResolveColors(ThemeMode.System);
    }

    public void SetMode(ThemeMode mode) => Mode = mode;

    public bool IsDark => Current.Background.GetBrightness() < 0.5f;

    public AppColors ResolveColors(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => LightTheme.Create(),
        ThemeMode.Dark => DarkTheme.Create(),
        _ => IsSystemDark() ? DarkTheme.Create() : LightTheme.Create()
    };

    public static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i) return i == 0;
        }
        catch { /* fallback */ }

        return SystemColors.Window.GetBrightness() < 0.5f;
    }

    public void ApplyTo(Control root)
    {
        ApplyRecursive(root, Current);
    }

    private static void ApplyRecursive(Control control, AppColors colors)
    {
        if (control is Panel or UserControl or Form)
        {
            if (control.Tag as string != "no-theme")
            {
                control.BackColor = colors.Background;
                control.ForeColor = colors.TextPrimary;
            }
        }

        if (control is TextBox tb)
        {
            tb.BackColor = colors.InputBg;
            tb.ForeColor = colors.TextPrimary;
            tb.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is ComboBox cb)
        {
            StyleComboBox(cb, colors);
        }
        else if (control is ListBox lb)
        {
            lb.BackColor = colors.Surface;
            lb.ForeColor = colors.TextPrimary;
            lb.BorderStyle = BorderStyle.None;
            lb.IntegralHeight = false;
            DrawingUtil.ApplyWindowChrome(lb);
        }
        else if (control is Button btn && btn is not Controls.ModernButton and not Controls.IconButton)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = colors.SurfaceAlt;
            btn.ForeColor = colors.TextPrimary;
            btn.FlatAppearance.MouseOverBackColor = colors.HoverBg;
        }
        else if (control is TabControl tabs)
        {
            tabs.BackColor = colors.Background;
            tabs.ForeColor = colors.TextPrimary;
            foreach (TabPage page in tabs.TabPages)
            {
                page.BackColor = colors.Background;
                page.ForeColor = colors.TextPrimary;
            }
        }
        else if (control is CheckBox chk)
        {
            chk.ForeColor = colors.TextPrimary;
            chk.BackColor = Color.Transparent;
        }
        else if (control is Label lbl && lbl is not Controls.TimerDisplay)
        {
            if (lbl.Tag as string != "no-theme")
                lbl.ForeColor = colors.TextPrimary;
        }
        else if (control is NumericUpDown nud)
        {
            nud.BackColor = colors.InputBg;
            nud.ForeColor = colors.TextPrimary;
            nud.BorderStyle = BorderStyle.FixedSingle;
        }
        else if (control is DateTimePicker dtp)
        {
            dtp.CalendarForeColor = colors.TextPrimary;
            dtp.CalendarMonthBackground = colors.Surface;
            dtp.BackColor = colors.InputBg;
            dtp.ForeColor = colors.TextPrimary;
        }
        else if (control is DataGridView dgv)
        {
            dgv.BackgroundColor = colors.Background;
            dgv.GridColor = colors.Border;
            dgv.DefaultCellStyle.BackColor = colors.Surface;
            dgv.DefaultCellStyle.ForeColor = colors.TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = colors.SelectedBg;
            dgv.DefaultCellStyle.SelectionForeColor = colors.TextPrimary;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = colors.SurfaceAlt;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = colors.TextPrimary;
            dgv.EnableHeadersVisualStyles = false;
        }
        else if (control is MenuStrip or StatusStrip or ToolStrip)
        {
            control.BackColor = colors.TopBarBg;
            control.ForeColor = colors.TextPrimary;
        }

        foreach (Control child in control.Controls)
            ApplyRecursive(child, colors);

        if (control is ListBox or ComboBox or DataGridView || control is ScrollableControl sc && sc.AutoScroll)
            DrawingUtil.ApplyWindowChrome(control);
    }

    private static void StyleComboBox(ComboBox cb, AppColors colors)
    {
        cb.BackColor = colors.InputBg;
        cb.ForeColor = colors.TextPrimary;
        cb.FlatStyle = FlatStyle.Flat;
        cb.IntegralHeight = false;
        if (cb is Controls.ThemedComboBox)
        {
            DrawingUtil.ApplyWindowChrome(cb);
            return;
        }

        if (cb.DrawMode != DrawMode.OwnerDrawFixed)
        {
            cb.DrawMode = DrawMode.OwnerDrawFixed;
            cb.ItemHeight = 26;
            cb.DrawItem += (_, e) => DrawComboItem(cb, e);
            cb.DropDown += (_, _) => DrawingUtil.ApplyComboDropDownTheme(cb);
        }
        DrawingUtil.ApplyWindowChrome(cb);
        cb.Invalidate();
    }

    private static void DrawComboItem(ComboBox cb, DrawItemEventArgs e)
    {
        var colors = Instance.Current;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(selected ? colors.SelectedBg : colors.InputBg);
        e.Graphics.FillRectangle(bg, e.Bounds);
        if (e.Index < 0) return;
        var text = cb.GetItemText(cb.Items[e.Index]);
        TextRenderer.DrawText(e.Graphics, text, UiMetrics.Body,
            Rectangle.Inflate(e.Bounds, -8, 0),
            selected ? colors.TextPrimary : colors.TextSecondary,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }
}
