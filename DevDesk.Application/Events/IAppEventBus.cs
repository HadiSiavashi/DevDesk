namespace DevDesk.Application.Events;

public interface IAppEventBus
{
    event EventHandler<AppEvent>? Published;
    void Publish(AppEventKind kind, Guid? entityId = null, object? payload = null);
}
