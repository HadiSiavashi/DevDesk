namespace DevDesk.WinForms.Themes;

/// <summary>Global UI scale (75–200%). Fonts, spacing, and control sizes read this factor.</summary>
public static class UiScale
{
    public const int MinPercent = 75;
    public const int MaxPercent = 200;
    public const int DefaultPercent = 125;
    public const string SettingKey = "UiScale";

    public static int Percent { get; private set; } = DefaultPercent;
    public static float Factor => Percent / 100f;

    public static event EventHandler? Changed;

    public static int Px(int designPx) => Math.Max(1, (int)Math.Round(designPx * Factor));

    public static float Stroke(float design = 1.5f) => Math.Max(1f, design * Factor);

    public static int Parse(string? raw)
    {
        if (int.TryParse(raw, out var percent))
            return Snap(percent);
        return DefaultPercent;
    }

    public static bool SetPercent(int percent)
    {
        percent = Snap(percent);
        if (percent == Percent)
            return false;

        Percent = percent;
        UiFonts.Rebuild();
        Changed?.Invoke(null, EventArgs.Empty);
        return true;
    }

    /// <summary>Subscribe until <paramref name="control"/> is disposed. No-ops if already disposed.</summary>
    public static void Attach(Control control, EventHandler handler)
    {
        void Wrapped(object? sender, EventArgs e)
        {
            if (control.IsDisposed || control.Disposing) return;
            handler(sender, e);
        }

        Changed += Wrapped;
        control.Disposed += (_, _) => Changed -= Wrapped;
    }

    private static int Snap(int percent)
    {
        percent = (int)Math.Round(percent / 5d) * 5;
        return Math.Clamp(percent, MinPercent, MaxPercent);
    }
}
