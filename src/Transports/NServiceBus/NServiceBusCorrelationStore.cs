using FluentSaga.Abstractions;

namespace FluentSaga.Transports.NServiceBus;

public class NServiceBusCorrelationStore : IFluentCorrelationStore
{
    public string CorrelationId { get; set; }
}