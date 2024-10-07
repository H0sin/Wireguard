namespace EventBus.Messages.Events;

public class IntegrationBaseEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreateDate { get; private set; } = DateTime.UtcNow;

    public IntegrationBaseEvent()
    {
    }

    public IntegrationBaseEvent(Guid id, DateTime createDate)
    {
        CreateDate = createDate;
        Id = id;
    }
}