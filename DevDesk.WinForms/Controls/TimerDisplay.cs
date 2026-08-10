using DevDesk.WinForms.Themes;

namespace DevDesk.WinForms.Controls;

public sealed class TimerDisplay : Label
{
    public TimerDisplay()
    {
        Font = UiMetrics.Timer;
        TextAlign = ContentAlignment.MiddleLeft;
        Height = 72;
        ThemeManager.Instance.ThemeChanged += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public void SetTime(TimeSpan elapsed)
    {
        Text = elapsed.ToString(elapsed.TotalHours >= 1 ? @"h\:mm\:ss" : @"mm\:ss");
    }

    public void SetMinutes(int minutes, int seconds = 0) => SetTime(new TimeSpan(0, minutes, seconds));

    private void ApplyTheme() => ForeColor = ThemeManager.Instance.Current.Accent;
}
