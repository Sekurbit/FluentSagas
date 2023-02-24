using FluentSaga.Abstractions;
using Microsoft.Extensions.Logging;

namespace FluentSaga.Transports.NServiceBus;

public class NServiceBusMessagePublisher : IFluentMessagePublisher
{
    private readonly IMessageSession _messageSession;
    private readonly ILogger<NServiceBusMessagePublisher> _logger;

    public NServiceBusMessagePublisher(IMessageSession messageSession, ILogger<NServiceBusMessagePublisher> logger)
    {
        _messageSession = messageSession;
        _logger = logger;
    }
    
    public async Task Publish<TMessage>(string sagaId, Type sagaType, TMessage message) where TMessage : IFluentEvent
    {
        // NServiceBus saga specific options needed so that we can follow this as a saga in Service Insight
        var options = new PublishOptions();
        options.SetHeader("NServiceBus.OriginatingSagaId", sagaId);
        options.SetHeader("NServiceBus.OriginatingSagaType", sagaType.AssemblyQualifiedName);

        options.SetMessageId(message.Id);
        options.SetHeader("NServiceBus.CorrelationId", message.CorrelationId);
        
        _logger.LogDebug($"Publishing {message.GetType()} for correlation ID '{message.CorrelationId}'");

        await _messageSession.Publish(message, options);
    }
}