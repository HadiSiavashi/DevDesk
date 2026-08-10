namespace DevDesk.WinForms.Services;

/// <summary>Single shared timer that drives lightweight WinForms animations.</summary>
public sealed class AnimationScheduler : IDisposable
{
    private static readonly Lazy<AnimationScheduler> Lazy = new(() => new AnimationScheduler());
    public static AnimationScheduler Instance => Lazy.Value;

    private readonly System.Windows.Forms.Timer _timer;
    private readonly List<AnimEntry> _entries = [];
    private bool _disposed;

    private AnimationScheduler()
    {
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += OnTick;
    }

    public void Animate(int durationMs, Action<float> onProgress, Action? onComplete = null, Func<float, float>? easing = null)
    {
        if (_disposed) return;
        durationMs = Math.Clamp(durationMs, 16, 2000);
        _entries.Add(new AnimEntry
        {
            DurationMs = durationMs,
            StartedAt = Environment.TickCount64,
            OnProgress = onProgress,
            OnComplete = onComplete,
            Easing = easing ?? EaseOutCubic
        });
        if (!_timer.Enabled) _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_entries.Count == 0)
        {
            _timer.Stop();
            return;
        }

        var now = Environment.TickCount64;
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            var t = (float)(now - entry.StartedAt) / entry.DurationMs;
            if (t >= 1f)
            {
                try { entry.OnProgress(1f); } catch { /* ignore */ }
                try { entry.OnComplete?.Invoke(); } catch { /* ignore */ }
                _entries.RemoveAt(i);
            }
            else
            {
                try { entry.OnProgress(entry.Easing(Math.Clamp(t, 0f, 1f))); } catch { /* ignore */ }
            }
        }
    }

    public static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);
    public static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
        _entries.Clear();
    }

    private sealed class AnimEntry
    {
        public required long StartedAt { get; init; }
        public required int DurationMs { get; init; }
        public required Action<float> OnProgress { get; init; }
        public Action? OnComplete { get; init; }
        public required Func<float, float> Easing { get; init; }
    }
}
