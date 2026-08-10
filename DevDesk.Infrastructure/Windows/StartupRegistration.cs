using System.Runtime.Versioning;
using DevDesk.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DevDesk.Infrastructure.Windows;

/// <summary>
/// Registers/unregisters Start with Windows via HKCU Run key. OFF by default (no auto-register).
/// </summary>
public sealed class StartupRegistration : IStartupRegistration
{
    public const string RunValueName = "DevDesk";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly ILogger<StartupRegistration> _logger;
    private readonly string _executablePath;

    public StartupRegistration(ILogger<StartupRegistration> logger, string? executablePath = null)
    {
        _logger = logger;
        _executablePath = string.IsNullOrWhiteSpace(executablePath)
            ? Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "DevDesk.WinForms.exe")
            : executablePath;
    }

    public bool IsRegistered
    {
        get
        {
            if (!OperatingSystem.IsWindows())
                return false;

            return IsRegisteredCore();
        }
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
            Register();
        else
            Unregister();
    }

    public void Register()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Start with Windows is only supported on Windows.");

        if (string.IsNullOrWhiteSpace(_executablePath) || !File.Exists(_executablePath))
            throw new InvalidOperationException(
                "Cannot register Start with Windows because the application executable path is missing.");

        try
        {
            RegisterCore();
            _logger.LogInformation("Registered DevDesk to start with Windows (current user).");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while registering Start with Windows.");
            throw new InvalidOperationException(
                "Could not enable Start with Windows due to insufficient permissions.",
                ex);
        }
    }

    public void Unregister()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            UnregisterCore();
            _logger.LogInformation("Unregistered DevDesk from Start with Windows.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while unregistering Start with Windows.");
            throw new InvalidOperationException(
                "Could not disable Start with Windows due to insufficient permissions.",
                ex);
        }
    }

    [SupportedOSPlatform("windows")]
    private bool IsRegisteredCore()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var value = key?.GetValue(RunValueName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read Start with Windows registration.");
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private void RegisterCore()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (key is null)
            throw new InvalidOperationException("Unable to open the current-user Run registry key.");

        // Quote path for safety with spaces; no elevation required (HKCU).
        key.SetValue(RunValueName, $"\"{_executablePath}\"");
    }

    [SupportedOSPlatform("windows")]
    private void UnregisterCore()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(RunValueName) is null)
            return;

        key.DeleteValue(RunValueName, throwOnMissingValue: false);
    }
}
