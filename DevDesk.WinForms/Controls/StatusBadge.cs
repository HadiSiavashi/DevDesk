using DevDesk.Domain.Enums;
using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class StatusBadge : Label
{
    public StatusBadge()
    {
        AutoSize = true;
        Padding = new Padding(6, 2, 6, 2);
        Font = new Font("Segoe UI", 7.5F);
    }

    public WorkTaskStatus Status
    {
        set
        {
            Text = value.ToString();
            var c = ThemeManager.Instance.Current;
            BackColor = value == WorkTaskStatus.Done ? c.Success : c.SurfaceAlt;
            ForeColor = value == WorkTaskStatus.Done ? Color.White : c.TextSecondary;
        }
    }
}
