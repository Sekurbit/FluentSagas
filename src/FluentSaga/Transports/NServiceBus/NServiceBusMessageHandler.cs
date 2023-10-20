using FluentSaga.Abstractions;
using Microsoft.Extensions.Logging;

namespace FluentSaga.Transports.NServiceBus;

/// <summary>
/// This class gets dynamically instantiated and added to the DI container so that NServiceBus finds it.
/// The instances are created based on what sagas exists and what events these sagas act on.
/// </summary>
/// <typeparam name="Event"></typeparam>
public class NServiceBusMessageHandler : IHandleMessages<FluentEvent>
{
    private readonly IFluentSagaRouter _sagaRouter;
    private readonly IFluentCorrelationStore _correlationStore;
    private readonly ILogger<NServiceBusMessageHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public NServiceBusMessageHandler(IFluentSagaRouter sagaRouter, IFluentCorrelationStore correlationStore,
        ILogger<NServiceBusMessageHandler> logger, IServiceProvider serviceProvider)
    {
        _sagaRouter = sagaRouter;
        _correlationStore = correlationStore;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Handle(FluentEvent message, IMessageHandlerContext context)
    {
        _logger.LogDebug($"{message.GetType()} received");

        // Append the correlation ID header to the message
        _correlationStore.CorrelationId = context.MessageHeaders["NServiceBus.CorrelationId"];
        message.CorrelationId = _correlationStore.CorrelationId;
        
        await _sagaRouter.ExecuteAsync(message, _serviceProvider);
    }
}