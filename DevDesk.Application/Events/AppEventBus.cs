namespace DevDesk.Application.Events;

public sealed class AppEventBus : IAppEventBus
{
    public event EventHandler<AppEvent>? Published;

    public void Publish(AppEventKind kind, Guid? entityId = null, object? payload = null)
    {
        var evt = new AppEvent
        {
            Kind = kind,
            EntityId = entityId,
            Payload = payload,
            OccurredAtUtc = DateTime.UtcNow
        };
        Published?.Invoke(this, evt);
    }
}
