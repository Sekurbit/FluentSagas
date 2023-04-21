using FluentSaga.Abstractions;
using FluentSaga.Transports;
using Microsoft.Extensions.DependencyInjection;
using SagaTest;

namespace FluentSaga;

public abstract class FluentSagaStepBuilder
{
    private readonly FluentSagaBuilder _sagaBuilder;

    public FluentSagaStepBuilder(FluentSagaBuilder sagaBuilder)
    {
        _sagaBuilder = sagaBuilder;
    }

    protected FluentSagaBuilder SagaBuilder => _sagaBuilder;
}

public class FluentSagaConditionStepBuilder<TInitiatorEvent> : FluentSagaStepBuilder
{
    public FluentSagaConditionStepBuilder(FluentSagaBuilder sagaBuilder) : base(sagaBuilder)
    {
    }

    public FluentSagaConditionStepBuilder<TInitiatorEvent> When(Func<TInitiatorEvent, bool> condition,
        Action<FluentSagaConditionStepBuilder<TInitiatorEvent>> buildStep)
    {
        var conditionalFunc = (Func<object, bool>) ((e) =>
        {
            if (!(e is TInitiatorEvent evt)) return false;
            return condition(evt);
        });
        
        var subSaga = new FluentSagaBuilder(SagaBuilder.ServiceProvider);
        var subStep = new FluentSagaInitiatorStepBuilder<TInitiatorEvent>(subSaga);
        buildStep(subStep);
        
        // Build the subsaga
        subSaga.Build();

        // Create the saga step
        var conditionalStep = new FluentConditionalSagaStep(conditionalFunc, subSaga.Steps);
        SagaBuilder.AddStep(conditionalStep);

        return this;
    }

    public FluentSagaConditionStepBuilder<TInitiatorEvent> Publish<TCommand>(
        Func<TInitiatorEvent, TCommand> handler)
    {
        var handlerFunc = (Func<object, object?>) ((e) =>
        {
            if (!(e is TInitiatorEvent evt)) return false;
            return handler(evt);
        });
        
        // Create saga step
        var publishStep = new FluentPublishSagaStep(
            SagaBuilder.ServiceProvider.GetService<IFluentMessagePublisher>()!,
            handlerFunc
        );
        SagaBuilder.AddStep(publishStep);
        
        return this;
    }

    public FluentSagaConditionStepBuilder<TInitiatorEvent> Publish<TCommandOrEvent>(TCommandOrEvent commandOrEvent)
    {
        // Create saga step
        var publishStep = new FluentPublishSagaStep(
            SagaBuilder.ServiceProvider.GetService<IFluentMessagePublisher>()!,
            commandOrEvent
        );
        SagaBuilder.AddStep(publishStep);
        
        return this;
    }

    public FluentSagaBuilder PublishAndContinue<TCommand>(
        Func<TInitiatorEvent, TCommand> handler)
    {
        // Create saga step
        Publish<TCommand>(handler);
        
        return SagaBuilder;
    }
    
    public FluentSagaBuilder PublishAndContinue<TCommandOrEvent>(TCommandOrEvent commandOrEvent)
    {
        // Create saga step
        Publish<TCommandOrEvent>(commandOrEvent);
        
        return SagaBuilder;
    }
    
    public FluentSagaConditionStepBuilder<TInitiatorEvent> Execute(Func<TInitiatorEvent, Task<bool>> execute)
    {
        // Create saga step
        var executeFunc = (Func<object, Task<bool>>) ((e) =>
        {
            if (!(e is TInitiatorEvent evt)) return Task.FromResult(true);
            return execute(evt);
        });

        var executeStep = new FluentExecuteSagaStep(executeFunc);
        SagaBuilder.AddStep(executeStep);
        
        return this;
    }

    public FluentSagaConditionStepBuilder<TInitiatorEvent> Execute<THandler>() where THandler : IFluentCommand
    {
        // Create saga step
        var executeFunc = (Func<IFluentEvent, Task<bool>>) ((e) =>
        {
            var instance = Activator.CreateInstance<THandler>();
            return instance.ExecuteAsync(e);
        });

        var executeStep = new FluentExecuteSagaStep(executeFunc);
        SagaBuilder.AddStep(executeStep);

        return this;
    }
    
    public FluentSagaBuilder ExecuteAndContinue(Func<TInitiatorEvent, Task<bool>> execute)
    {
        // Create saga step
        Execute(execute);
        
        return SagaBuilder;
    }
    
    public FluentSagaBuilder ExecuteAndContinue<THandler>() where THandler : IFluentCommand
    {
        // Create saga step
        Execute<THandler>();
        
        return SagaBuilder;
    }

    /// <summary>
    /// Completes the saga for the given CorrelationId
    /// </summary>
    /// <returns></returns>
    public FluentSagaBuilder Empty()
    {
        // Do nothing, we're done.
        
        return SagaBuilder;
    }

    /// <summary>
    /// Halts the execution of the saga and forces the message to be left on the queue for another retry.
    /// </summary>
    /// <param name="message">Message to provide to the exception that will be thrown</param>
    /// <param name="inner">Inner exception if needed</param>
    /// <returns>Nothing</returns>
    /// <exception cref="SagaException">Thrown by executing this function</exception>
    public FluentSagaBuilder Throw(string message, Exception? inner = null)
    {
        throw new SagaException(message, inner);
    }

    public FluentSagaConditionStepBuilder<TInitiatorEvent> Ensure<THandler>(Action<FluentSagaValidationStepBuilder<TInitiatorEvent>> buildStep) where THandler : IFluentPromiseExecutor
    {
        // Create the saga step
        var validator = (IFluentPromiseExecutor) InstanceHelper.Create(SagaBuilder.ServiceProvider, typeof(THandler));
        
        var subSagaBuilder = new FluentSagaBuilder(SagaBuilder.ServiceProvider);
        var subStep = new FluentSagaValidationStepBuilder<TInitiatorEvent>(subSagaBuilder);
        buildStep(subStep);
        
        // Build the subsaga
        var subSaga = subSagaBuilder.Build();
        
        var ensureStep = new FluentPromiseSagaStep(validator, subSaga);
        SagaBuilder.AddStep(ensureStep);

        return this;
    }
}

public class FluentSagaValidationStepBuilder<TInitiatorEvent> : FluentSagaStepBuilder
{
    public FluentSagaValidationStepBuilder(FluentSagaBuilder sagaBuilder)
        : base(sagaBuilder)
    {
    }

    public FluentSagaValidationStepBuilder<TInitiatorEvent> OnSuccess(Action<FluentSagaInitiatorStepBuilder<TInitiatorEvent>> buildStep)
    {
        // Create saga step
        var subSagaBuilder = new FluentSagaBuilder(SagaBuilder.ServiceProvider);
        var subStep = new FluentSagaInitiatorStepBuilder<TInitiatorEvent>(subSagaBuilder);
        buildStep(subStep);

        var subSaga = subSagaBuilder.Build();

        var step = new FluentPromiseSuccessSagaStep(subSaga);
        SagaBuilder.AddStep(step);

        return this;
    }
    
    public FluentSagaBuilder OnFailure(Action<FluentSagaInitiatorStepBuilder<TInitiatorEvent>> buildStep)
    {
        // Create saga step
        var subSagaBuilder = new FluentSagaBuilder(SagaBuilder.ServiceProvider);
        var subStep = new FluentSagaInitiatorStepBuilder<TInitiatorEvent>(subSagaBuilder);
        buildStep(subStep);

        var subSaga = subSagaBuilder.Build();

        var step = new FluentPromiseFailureSagaStep(subSaga);
        SagaBuilder.AddStep(step);
        
        return SagaBuilder;
    }
}

public class FluentSagaInitiatorStepBuilder<TInitiatorEvent> : FluentSagaConditionStepBuilder<TInitiatorEvent>
{
    public FluentSagaInitiatorStepBuilder(FluentSagaBuilder sagaBuilder) : base(sagaBuilder)
    {
    }
}