namespace FluentSaga.State;

public class MemoryStatePersistence : IFluentSagaStatePersistence
{
    private List<object> _states = new ();
    
    public Task<TStateType?> LoadAsync<TStateType>(string sagaId) where TStateType : IFluentSagaState
    {
        return Task.FromResult((TStateType)_states.OfType<IFluentSagaState>().FirstOrDefault(x => x.SagaId.Equals(sagaId))!)!;
    }

    public Task<object?> LoadAsync(Type sagaStateType, string sagaId)
    {
        return Task.FromResult<object?>(_states.OfType<IFluentSagaState>().FirstOrDefault(x => x.SagaId.Equals(sagaId)));
    }

    public Task SaveAsync(string sagaId, object state)
    {
        _states.Add(state);
        return Task.CompletedTask;
    }

    public Task CompleteAsync(string sagaId)
    {
        _states.RemoveAll(x => x is IFluentSagaState state && state.SagaId.Equals(sagaId));
        return Task.CompletedTask;
    }
}