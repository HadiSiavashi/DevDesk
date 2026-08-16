using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace DevDesk.WinForms.Themes;

/// <summary>Loads embedded Geist / JetBrains Mono and exposes cached UI fonts.</summary>
public static class UiFonts
{
    private static readonly PrivateFontCollection Collection = new();
    private static readonly List<IntPtr> _keptBuffers = [];
    private static bool _initialized;

    private static FontFamily _sans = FontFamily.GenericSansSerif;
    private static FontFamily _sansSemi = FontFamily.GenericSansSerif;
    private static FontFamily _mono = FontFamily.GenericMonospace;
    private static FontFamily _monoMed = FontFamily.GenericMonospace;

    public static Font PageTitle { get; private set; } = new("Segoe UI Semibold", 14.25F, FontStyle.Bold);
    public static Font SectionTitle { get; private set; } = new("Segoe UI Semibold", 10.5F, FontStyle.Bold);
    public static Font Body { get; private set; } = new("Segoe UI", 9.75F);
    public static Font BodySemi { get; private set; } = new("Segoe UI Semibold", 9.75F);
    public static Font Meta { get; private set; } = new("Segoe UI", 8.25F);
    public static Font Caption { get; private set; } = new("Segoe UI", 8.25F);
    public static Font Mono { get; private set; } = new("Consolas", 9F);
    public static Font MonoTimer { get; private set; } = new("Consolas", 10.5F, FontStyle.Bold);
    public static Font TimerGiant { get; private set; } = new("Consolas", 48F, FontStyle.Bold);
    public static Font TimerReady { get; private set; } = new("Consolas", 36F, FontStyle.Bold);
    public static Font StatValue { get; private set; } = new("Consolas", 18F, FontStyle.Bold);
    public static Font Kbd { get; private set; } = new("Consolas", 7.5F);

    public static FontFamily Sans => _sans;
    public static FontFamily MonoFamily => _mono;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        TryAdd("DevDesk.WinForms.Assets.Fonts.Geist-Regular.ttf", ref _sans);
        TryAdd("DevDesk.WinForms.Assets.Fonts.Geist-SemiBold.ttf", ref _sansSemi);
        TryAdd("DevDesk.WinForms.Assets.Fonts.JetBrainsMono-Regular.ttf", ref _mono);
        TryAdd("DevDesk.WinForms.Assets.Fonts.JetBrainsMono-Medium.ttf", ref _monoMed);

        var rtl = false;
        try { rtl = Localization.LocalizationService.Instance.IsRtl; } catch { /* catalog may not be ready */ }

        var linePad = rtl ? 1.2f : 1f;
        PageTitle = New(_sansSemi, 14.25F * linePad, FontStyle.Bold);
        SectionTitle = New(_sansSemi, 10.5F * linePad, FontStyle.Bold);
        Body = New(_sans, 9.75F * linePad, FontStyle.Regular);
        BodySemi = New(_sansSemi, 9.75F * linePad, FontStyle.Bold);
        Meta = New(_sans, 8.25F * linePad, FontStyle.Regular);
        Caption = New(_sans, 8.25F * linePad, FontStyle.Regular);
        Mono = New(_mono, 9F, FontStyle.Regular);
        MonoTimer = New(_monoMed, 10.5F, FontStyle.Bold);
        TimerGiant = New(_monoMed, 48F, FontStyle.Bold);
        TimerReady = New(_monoMed, 36F, FontStyle.Bold);
        StatValue = New(_monoMed, 18F, FontStyle.Bold);
        Kbd = New(_mono, 7.5F, FontStyle.Regular);
    }

    public static void RefreshForCulture()
    {
        _initialized = false;
        Initialize();
    }

    private static Font New(FontFamily family, float size, FontStyle style)
    {
        try { return new Font(family, size, style, GraphicsUnit.Point); }
        catch { return new Font("Segoe UI", size, style, GraphicsUnit.Point); }
    }

    private static void TryAdd(string resourceName, ref FontFamily family)
    {
        try
        {
            var asm = typeof(UiFonts).Assembly;
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream is null) return;

            var bytes = new byte[stream.Length];
            stream.ReadExactly(bytes);
            var ptr = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            _keptBuffers.Add(ptr);

            uint installed = 1;
            DrawingUtil.AddFontMemResourceEx(ptr, (uint)bytes.Length, IntPtr.Zero, ref installed);
            Collection.AddMemoryFont(ptr, bytes.Length);
            if (Collection.Families.Length > 0)
                family = Collection.Families[^1];
        }
        catch
        {
            /* keep fallback family */
        }
    }
}
