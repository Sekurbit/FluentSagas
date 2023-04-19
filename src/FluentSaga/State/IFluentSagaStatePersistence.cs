namespace FluentSaga.State;

public interface IFluentSagaStatePersistence
{
    Task<TStateType?> LoadAsync<TStateType>(string sagaId) where TStateType : IFluentSagaState;

    Task<object?> LoadAsync(Type sagaStateType, string sagaId);

    Task SaveAsync(string sagaId, object state);

    Task CompleteAsync(string sagaId);
}