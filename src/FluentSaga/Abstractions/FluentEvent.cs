namespace FluentSaga.Abstractions;

public abstract class FluentEvent : IFluentEvent, NServiceBus.IEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; set; }
    public string SagaId { get; set; }
}