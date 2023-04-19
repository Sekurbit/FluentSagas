namespace FluentSaga.Abstractions;

public interface IFluentPromiseExecutor
{
    Task<bool> ExecuteAsync<TInitiatorEvent>(TInitiatorEvent @event);
}