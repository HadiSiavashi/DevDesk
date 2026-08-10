namespace DevDesk.Application.Interfaces;

/// <summary>
/// Registers/unregisters DevDesk to start with Windows (OFF by default).
/// </summary>
public interface IStartupRegistration
{
    bool IsRegistered { get; }
    void Register();
    void Unregister();
    void SetEnabled(bool enabled);
}
