using FluentSaga.State;
using Microsoft.Extensions.Logging;

namespace FluentSaga;

public abstract class FluentSaga
{
    private readonly ILogger _logger;
    private bool _isConfigured = false;
    private bool _isExceptionsMuted = false;
    
    private IList<Type> _entryEventTypes = new List<Type>();
    private IList<FluentSagaStep> _saga = new List<FluentSagaStep>();
    private IDictionary<Type, Action<object>> _exceptionHandlers = new Dictionary<Type, Action<object>>();
    private IList<FluentSagaStep> _executionPath = new List<FluentSagaStep>();

    public FluentSaga(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Configures the saga with each step that is included in the execution path
    /// </summary>
    /// <param name="sagaBuilder"></param>
    public virtual void OnConfigure(FluentSagaBuilder sagaBuilder)
    {
        if (!sagaBuilder.IsBuilt)
            sagaBuilder.Build();

        _saga = sagaBuilder.Steps;
        MapStepsToSaga(_saga);

        _entryEventTypes = sagaBuilder.EntryTypes;
        _isExceptionsMuted = sagaBuilder.IsExceptionsMuted;
        _exceptionHandlers = sagaBuilder.ExceptionHandlers;
        
        _isConfigured = true;
    }
    
    /// <summary>
    /// Maps a reference to this instance to each step in the saga
    /// </summary>
    /// <param name="steps"></param>
    private void MapStepsToSaga(IList<FluentSagaStep> steps)
    {
        foreach (var step in steps)
        {
            step.Saga = this;
            MapStepsToSaga(step.SubSteps);
        }
    }

    /// <summary>
    /// Executes the saga
    /// </summary>
    /// <param name="event">Event to provide to the saga</param>
    public async Task RunAsync(Abstractions.IFluentEvent @event)
    {
        // Execute the saga!
        foreach (var step in _saga)
        {
            try
            {
                if (!(await step.ExecuteAsync(@event)))
                {
                    _logger.LogInformation(
                        $"Saga with correlation id '{@event.CorrelationId}' validated false on step {_saga.IndexOf(step)}.");
                    break;
                }

                _executionPath.Add(step);
            }
            catch (Exception ex)
            {
                if (IsExceptionsMuted)
                {
                    var exceptionHandler = _exceptionHandlers.FirstOrDefault(x => x.Key == ex.GetType());
                    if (!exceptionHandler.Equals(default))
                        exceptionHandler.Value(ex);
                    else
                    {
                        _logger.LogCritical(
                            $"Unhandled exception '{ex.GetType()}' was thrown and IsExceptionsMuted is set to true. You should specify an OnException handler in the saga to handle this error.",
                            ex);
                    }
                }
                else
                {
                    var exceptionHandler = _exceptionHandlers.FirstOrDefault(x => x.Key == ex.GetType());
                    if (!exceptionHandler.Equals(default))
                        exceptionHandler.Value(ex);
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
    
    public T? Get<T>() where T : FluentSagaStep
    {
        return (T?)_saga.FirstOrDefault(x => x.GetType() == typeof(T));
    }

    /// <summary>
    /// Gets the unique ID of this instance of the saga
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets a read only version of the executed path of the saga
    /// </summary>
    public IReadOnlyList<FluentSagaStep> ExecutedPath => _executionPath.ToList();

    internal bool IsConfigured => _isConfigured;

    /// <summary>
    /// Gets a flag that defines if the saga has muted exceptions, meaning that once exceptions occur, the system will only be
    /// notified via logging or explicit handling of exceptions.
    /// </summary>
    public bool IsExceptionsMuted => _isExceptionsMuted;

    /// <summary>
    /// Gets the list of types the triggers this saga
    /// </summary>
    internal IList<Type> EntryEventTypes => _entryEventTypes;
}

public abstract class FluentSaga<TSagaState> : FluentSaga where TSagaState : IFluentSagaState
{
    protected FluentSaga(ILogger logger) : base(logger)
    {
        State = (TSagaState)Activator.CreateInstance(typeof(TSagaState))!;
    }

    public void SetInitialState(TSagaState state)
    {
        State = state;
    }
    
    public TSagaState State
    {
        get;
        internal set;
    }
}