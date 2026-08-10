using DevDesk.Domain.Enums;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class PriorityBadge : Label
{
    public PriorityBadge()
    {
        AutoSize = true;
        Padding = new Padding(6, 2, 6, 2);
        Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
    }

    public TaskPriority Priority
    {
        set
        {
            Text = value.ToString();
            ForeColor = value switch
            {
                TaskPriority.Critical => Color.White,
                TaskPriority.High => Color.White,
                _ => ThemeManager.Instance.Current.TextPrimary
            };
            BackColor = value switch
            {
                TaskPriority.Critical => Color.FromArgb(220, 38, 38),
                TaskPriority.High => Color.FromArgb(234, 88, 12),
                TaskPriority.Medium => Color.FromArgb(56, 189, 248),
                _ => Color.FromArgb(100, 116, 139)
            };
        }
    }
}
