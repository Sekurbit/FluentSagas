using FluentSaga.Abstractions;
using FluentSaga.Transports;
using SagaTest.Extensions;

namespace FluentSaga;

public abstract class FluentSagaStep
{
    public abstract Task<bool> ExecuteAsync<TEvent>(TEvent @event) where TEvent : Abstractions.IFluentEvent;

    public IList<FluentSagaStep> SubSteps { get; set; } = new List<FluentSagaStep>();
    
    internal global::FluentSaga.FluentSaga Saga { get; set; }
}

public class FluentConditionalSagaStep : FluentSagaStep
{
    private readonly Func<Abstractions.IFluentEvent, bool> _condition;

    public FluentConditionalSagaStep(Func<Abstractions.IFluentEvent, bool> condition, IList<FluentSagaStep> subSteps)
    {
        _condition = condition;
        SubSteps = subSteps;
    }
    
    public override async Task<bool> ExecuteAsync<TInitiatorEvent>(TInitiatorEvent @event)
    {
        if (_condition(@event))
        {
            foreach (var step in SubSteps)
            {
                if (!await step.ExecuteAsync(@event))
                    break;
            }
        }
        else
            return false;

        return true;
    }
}

public class FluentPublishSagaStep : FluentSagaStep
{
    private readonly Func<IFluentEvent, object?>? _handler;
    private readonly IFluentMessagePublisher _messagePublisher;
    private readonly object? _objectToPublish;

    public FluentPublishSagaStep(IFluentMessagePublisher messagePublisher, object? objectToPublish)
    {
        _messagePublisher = messagePublisher;
        _objectToPublish = objectToPublish;
    }

    public FluentPublishSagaStep(IFluentMessagePublisher messagePublisher, Func<Abstractions.IFluentEvent, object?> handler)
    {
        _messagePublisher = messagePublisher;
        _handler = handler;
    }
    
    public override async Task<bool> ExecuteAsync<TEvent>(TEvent @event)
    {
        if (_handler != null)
        {
            var output = _handler(@event);
            if (output is not IFluentEvent evt) return true;

            evt.CorrelationId = @event.CorrelationId;
            await _messagePublisher.Publish(Saga.Id, Saga.GetType(), evt);
        }
        else if (_objectToPublish != null)
        {
            if (_objectToPublish is not IFluentEvent evt) return true;
            
            evt.CorrelationId = @event.CorrelationId;
            await _messagePublisher.Publish(Saga.Id, Saga.GetType(), evt);
        }
        else
        {
            await _messagePublisher.Publish(Saga.Id, Saga.GetType(), @event);
        }

        return true;
    }
}

public class FluentExecuteSagaStep : FluentSagaStep
{
    private readonly Func<IFluentEvent, Task<bool>> _execute;

    public FluentExecuteSagaStep(Func<IFluentEvent, Task<bool>> execute)
    {
        _execute = execute;
    }
    
    public override async Task<bool> ExecuteAsync<TEvent>(TEvent @event)
    {
        return await _execute(@event);
    }
}

public class FluentCompletedBySagaStep : FluentSagaStep
{
    private readonly Func<Task<bool>> _execute;

    public FluentCompletedBySagaStep(Func<Task<bool>> execute)
    {
        _execute = execute;
    }
    
    public override async Task<bool> ExecuteAsync<TEvent>(TEvent @event)
    {
        return await _execute();
    }
}

public class FluentPromiseSagaStep : FluentSagaStep
{
    private readonly IFluentPromiseExecutor _promiseExecutor;

    public FluentPromiseSagaStep(IFluentPromiseExecutor promiseExecutor, IList<FluentSagaStep> subSaga)
    {
        _promiseExecutor = promiseExecutor;
        SubSteps = subSaga;
    }
    
    public override async Task<bool> ExecuteAsync<TEvent>(TEvent @event)
    {
        var success = SubSteps.Get<FluentPromiseSuccessSagaStep>();
        var failure = SubSteps.Get<FluentPromiseFailureSagaStep>();

        if (success == null || failure == null)
            throw new InvalidOperationException("Ensure saga step must declare both OnSuccess and OnFailure");

        try
        {
            if (await _promiseExecutor.ExecuteAsync(@event))
                return await success.ExecuteAsync(@event);
            
            return await failure.ExecuteAsync(@event);
        }
        catch (Exception ex)
        {
            if (Saga.IsExceptionsMuted)
            {
                failure.Error = ex;
                await failure.ExecuteAsync(@event);
            }
            else
            {
                throw;
            }

            return false;
        }
    }
}

public abstract class FluentLoopSagaStep : FluentSagaStep
{
    public FluentLoopSagaStep(IList<FluentSagaStep> steps)
    {
        SubSteps = steps;
    }
    
    public override async Task<bool> ExecuteAsync<TEvent>(TEvent @event)
    {
        foreach (var step in SubSteps)
        {
            if (!(await step.ExecuteAsync(@event)))
                return false;
        }

        return true;
    }
}

public class FluentPromiseSuccessSagaStep : FluentLoopSagaStep
{
    public FluentPromiseSuccessSagaStep(IList<FluentSagaStep> steps) : base(steps)
    {
    }
}

public class FluentPromiseFailureSagaStep : FluentLoopSagaStep
{
    public Exception? Error { get; set; }

    public FluentPromiseFailureSagaStep(IList<FluentSagaStep> steps) : base(steps)
    {
    }
}

public class FluentOnSagaStep : FluentSagaStep
{
    private readonly Type _runType;

    public FluentOnSagaStep(IList<FluentSagaStep> subSaga, Type runType)
    {
        SubSteps = subSaga;
        _runType = runType;
    }
    
    public override async Task<bool> ExecuteAsync<TEvent>(TEvent @event)
    {
        if (@event.GetType() == _runType)
        {
            foreach (var step in SubSteps)
            {
                if (!(await step.ExecuteAsync(@event)))
                    return false;
            }
        }
        
        return true;
    }
}