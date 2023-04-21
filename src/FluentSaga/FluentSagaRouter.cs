using System.Diagnostics;
using FluentSaga.Abstractions;
using FluentSaga.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SagaTest;

namespace FluentSaga;

public interface IFluentSagaRouter
{
    Task InitializeAsync();

    Task ExecuteAsync(Abstractions.IFluentEvent @event);
}

public class FluentSagaRouter : IFluentSagaRouter
{
    private readonly FluentSaga? _sagaToRun;
    private readonly ILogger<FluentSagaRouter> _logger;
    private readonly IFluentSagaStatePersistence _statePersistence;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDictionary<Type, List<Type>> _eventToSagaMapper = new Dictionary<Type, List<Type>>();

    public FluentSagaRouter(ILogger<FluentSagaRouter> logger, IFluentSagaStatePersistence statePersistence, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _statePersistence = statePersistence;
        _serviceProvider = serviceProvider;
    }

    public FluentSagaRouter(global::FluentSaga.FluentSaga saga, ILogger<FluentSagaRouter> logger, IFluentSagaStatePersistence statePersistence)
    {
        _sagaToRun = saga;
        _logger = logger;
        _statePersistence = statePersistence;
    }

    public Task InitializeAsync()
    {
        var sagaType = typeof(global::FluentSaga.FluentSaga);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic);

        foreach (var assembly in assemblies)
        {
            var types = assembly.ExportedTypes
                .Where(x => sagaType.IsAssignableFrom(x) && x is {IsClass: true, IsAbstract: false});

            foreach (var saga in types)
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var instance = (global::FluentSaga.FluentSaga) InstanceHelper.Create(serviceScope.ServiceProvider, saga);
                InitializeSagaInstance(instance);

                foreach (var entryType in instance.EntryEventTypes)
                {
                    if (_eventToSagaMapper.ContainsKey(entryType))
                    {
                        // Event is already registered with another saga, add this one to the list as well.
                        // We could potentially have one event trigger two sagas that performs different tasks
                        // when a certain event is received.
                        _eventToSagaMapper[entryType].Add(saga);
                    }
                    else
                    {
                        // First time we see this, add it to the dictionary.
                        _eventToSagaMapper.Add(entryType, new List<Type> {saga});
                    }
                }
            }
        }

        _logger.LogInformation($"Registered {_eventToSagaMapper.Keys.Count} event(s) that triggers {_eventToSagaMapper.Values.Sum(x => x.Count)} saga(s).");
        
        return Task.CompletedTask;
    }

    private void InitializeSagaInstance(global::FluentSaga.FluentSaga instance)
    {
        var builder = new FluentSagaBuilder(_serviceProvider);
        
        instance.OnConfigure(builder);
        
        if (!instance.IsConfigured)
            throw new InvalidOperationException("base.OnConfigure(sagaBuilder) must be called at the end of the overridden OnConfigure(...) method.");
    }

    public async Task ExecuteAsync(Abstractions.IFluentEvent @event)
    {
        if (!_eventToSagaMapper.Any())
            await InitializeAsync();

        var sagaTasks = new List<Task>();
        
        var timer = new Stopwatch();
        timer.Start();
        
        if (_sagaToRun != null)
        {
            await ExecuteSingleSaga(@event, _sagaToRun, _sagaToRun.GetType(), sagaTasks);
        }
        
        if (_sagaToRun == null && _eventToSagaMapper.TryGetValue(@event.GetType(), out var sagas))
        {
            _logger.LogInformation($"Got {sagas.Count} saga(s) to execute for the event of type {@event.GetType()}.");
            
            foreach (var saga in sagas)
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var instance = (global::FluentSaga.FluentSaga)InstanceHelper.Create(serviceScope.ServiceProvider, saga);
                await ExecuteSingleSaga(@event, instance, saga, sagaTasks);
            }
        }
        
        await Task.WhenAll(sagaTasks.ToArray());
        timer.Stop();
            
        _logger.LogInformation($"Sagas executed in {timer.Elapsed.ToString(@"m\:ss\.fff")}");
    }

    private async Task ExecuteSingleSaga(IFluentEvent @event, global::FluentSaga.FluentSaga instance, Type saga, List<Task> sagaTasks)
    {
        var stateProperty = instance.GetType().GetProperty("State");

        if (!string.IsNullOrEmpty(@event.SagaId))
            instance.Id = @event.SagaId;

        if (stateProperty != null)
        {
            var state = await _statePersistence.LoadAsync(stateProperty.PropertyType, instance.Id);
            
            if (state != null)
            {
                stateProperty.SetValue(instance, state);
            }
            else
            {
                state = stateProperty.GetValue(instance);
                var sagaIdProperty = state!.GetType().GetProperty("SagaId");
                sagaIdProperty!.SetValue(state, instance.Id);
            }
        }

        InitializeSagaInstance(instance);

        sagaTasks.Add(Task.Run(async () =>
        {
            // Run the saga
            await instance.RunAsync(@event);

            // Persist any state data
            var completedStep = instance.Get<FluentCompletedBySagaStep>();
            if (completedStep != null)
            {
                var isCompleted = await completedStep.ExecuteAsync(@event);
                if (!isCompleted)
                {
                    // Ok, the saga is not completed yet, so we need to persist the state so that we in the
                    // future can resume the saga with the current state data.
                    await PersistCurrentState(saga, instance);
                }
                else
                {
                    await _statePersistence.CompleteAsync(instance.Id);
                }
            }
            else
            {
                // No completed by condition found, we persist the state since we don't know when it is
                // actually completed
                var state = GetState(instance);
                if (state is {Completed: true})
                {
                    await _statePersistence.CompleteAsync(instance.Id);
                }
                else if (state is not null)
                {
                    await PersistCurrentState(saga, instance);
                }
            }
        }));
    }

    private IFluentSagaState? GetState(global::FluentSaga.FluentSaga instance)
    {
        var stateProperty = instance.GetType().GetProperty("State");
        if (stateProperty != null)
            return (IFluentSagaState?)stateProperty?.GetValue(instance) ?? null;

        return null;
    }

    private async Task PersistCurrentState(Type saga, global::FluentSaga.FluentSaga instance)
    {
        var stateProperty = saga.GetProperty("State");
        if (stateProperty != null)
        {
            var state = stateProperty.GetValue(instance);
            await _statePersistence.SaveAsync(instance.Id, state as IFluentSagaState);
        }
    }
}