using SagaTest;

namespace FluentSaga;

public class FluentSagaBuilder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IList<FluentSagaStep> _steps = new List<FluentSagaStep>();
    private readonly IList<Type> _entryTypes = new List<Type>();
    private readonly IDictionary<Type, Action<object>> _exceptionHandlers = new Dictionary<Type, Action<object>>();

    internal FluentSagaBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Starts a new data flow in the saga, based in the trigger. This makes it so that we can have multiple data flows
    /// based on what event that triggers this saga.
    /// </summary>
    /// <param name="stepBuilder">Construct the saga that should be executed when the given event has triggered the saga</param>
    /// <typeparam name="TInitiatorEvent">Event that triggers the saga</typeparam>
    /// <returns>The saga builder</returns>
    /// <exception cref="InvalidOperationException">Thrown if the saga has already been built</exception>
    public FluentSagaBuilder On<TInitiatorEvent>(Action<FluentSagaConditionStepBuilder<TInitiatorEvent>> stepBuilder)
    {
        if (IsBuilt)
            throw new InvalidOperationException("The saga has already been built.");
        
        _entryTypes.Add(typeof(TInitiatorEvent));
        
        var subSagaBuilder = new FluentSagaBuilder(ServiceProvider);
        var subStep = new FluentSagaConditionStepBuilder<TInitiatorEvent>(subSagaBuilder);
        stepBuilder(subStep);
        
        // Build the subsaga
        var subSaga = subSagaBuilder.Build();

        var step = new FluentOnSagaStep(subSaga, typeof(TInitiatorEvent));
        AddStep(step);
        
        return this;
    }

    public FluentSagaBuilder OnException<TException>(Action<TException> exceptionHandler) where TException : Exception
    {
        _exceptionHandlers.Add(typeof(TException), (e) =>
        {
            if (e is TException ex) exceptionHandler(ex);
        });
        
        return this;
    }

    /// <summary>
    /// Mutes exceptions, meaning that the execution of the saga will not throw exceptions uncontrollably if this is
    /// enabled. You can gracefully catch them by using the OnException fluent method.
    /// </summary>
    /// <returns>The saga builder</returns>
    public FluentSagaBuilder MuteExceptions()
    {
        if (IsBuilt)
            throw new InvalidOperationException("The saga has already been built.");
        
        IsExceptionsMuted = true;
        return this;
    }
    
    public FluentSagaBuilder CompletedBy(Func<Task<bool>> completedBy)
    {
        // Create saga step
        var executeStep = new FluentCompletedBySagaStep(completedBy);
        AddStep(executeStep);
        
        return this;
    }
    
    /// <summary>
    /// Adds a step to the saga
    /// </summary>
    /// <param name="step">Step to add to the saga</param>
    internal void AddStep(FluentSagaStep step)
    {
        if (IsBuilt)
            throw new InvalidOperationException("The saga has already been built.");
        
        Steps.Add(step);
    }

    /// <summary>
    /// Build the complete saga (no changes can be done after this)
    /// </summary>
    /// <returns></returns>
    public IList<FluentSagaStep> Build()
    {
        // Set saga to built
        IsBuilt = true;
        
        return Steps;
    }
    
    /// <summary>
    /// Gets a value indicating if the saga has been built yet or not. After the build is complete, no more steps
    /// can be added to the saga.
    /// </summary>
    internal bool IsBuilt { get; private set; }
    
    /// <summary>
    /// Gets a value indicating if the saga has muted uncontrolled exceptions
    /// </summary>
    internal bool IsExceptionsMuted { get; set; }

    /// <summary>
    /// Gets a list of the steps in the saga
    /// </summary>
    internal IList<FluentSagaStep> Steps => _steps;

    /// <summary>
    /// Gets a list of types that can trigger this saga
    /// </summary>
    internal IList<Type> EntryTypes => _entryTypes;

    /// <summary>
    /// Gets a list of exception handlers
    /// </summary>
    internal IDictionary<Type, Action<object>> ExceptionHandlers => _exceptionHandlers;

    /// <summary>
    /// Gets a reference to the DI container
    /// </summary>
    internal IServiceProvider ServiceProvider => _serviceProvider;
}