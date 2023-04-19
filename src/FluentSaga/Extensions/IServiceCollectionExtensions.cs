using FluentSaga.Abstractions;
using FluentSaga.State;
using FluentSaga.Transports;
using FluentSaga.Transports.NServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSaga.Extensions;

public class FluentSagasOptionsBuilder
{
    private readonly IServiceCollection _services;

    public FluentSagasOptionsBuilder(IServiceCollection services)
    {
        _services = services;
    }
    
    public FluentSagasOptionsBuilder UseNServiceBus()
    {
        // Can only live per request scope
        _services.AddTransient<IHandleMessages<FluentEvent>, NServiceBusMessageHandler>();
        _services.AddTransient<IFluentMessagePublisher, NServiceBusMessagePublisher>();
        _services.AddTransient<IFluentCorrelationStore, NServiceBusCorrelationStore>();

        return this;
    }

    public FluentSagasOptionsBuilder UseFileStatePersistence()
    {
        _services.AddSingleton<IFluentSagaStatePersistence, FileStatePersistence>();
        return this;
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFluentSagas(this IServiceCollection services, Action<FluentSagasOptionsBuilder> options)
    {
        services.AddSingleton<IFluentSagaRouter, FluentSagaRouter>();

        var optionsBuilder = new FluentSagasOptionsBuilder(services);
        options(optionsBuilder);

        return services;
    }
}