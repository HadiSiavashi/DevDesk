namespace DevDesk.WinForms.Services;

public interface INavigationHost
{
    void Navigate(string viewKey, object? parameter = null);
    void NavigateBack();
    string? CurrentViewKey { get; }
}

public sealed class NavigationService
{
    private readonly Dictionary<string, Func<object?, UserControl>> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<(string Key, object? Param)> _history = new();
    private object? _lastParam;

    public INavigationHost? Host { get; set; }

    public event EventHandler<string>? ViewChanged;

    public void Register(string key, Func<object?, UserControl> factory) => _factories[key] = factory;

    public void Navigate(string viewKey, object? parameter = null, bool pushHistory = true)
    {
        if (Host is null) return;
        if (pushHistory && Host.CurrentViewKey is not null)
            _history.Push((Host.CurrentViewKey, _lastParam));
        _lastParam = parameter;
        Host.Navigate(viewKey, parameter);
        ViewChanged?.Invoke(this, viewKey);
    }

    public void NavigateBack()
    {
        if (_history.Count == 0 || Host is null) return;
        var (key, param) = _history.Pop();
        _lastParam = param;
        Host.Navigate(key, param);
        ViewChanged?.Invoke(this, key);
    }

    public UserControl CreateView(string key, object? parameter = null)
    {
        if (!_factories.TryGetValue(key, out var factory))
            throw new InvalidOperationException($"View '{key}' is not registered.");
        return factory(parameter);
    }

    public void ClearHistory() => _history.Clear();
}
