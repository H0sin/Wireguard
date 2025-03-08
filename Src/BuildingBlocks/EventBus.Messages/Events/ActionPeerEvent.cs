namespace EventBus.Messages.Events;

public class ActionPeerEvent : IntegrationBaseEvent
{
    public Task Action { get; set; }
}