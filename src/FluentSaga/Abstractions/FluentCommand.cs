namespace FluentSaga.Abstractions;

public abstract class FluentCommand : IFluentCommand
{
    public abstract Task<bool> ExecuteAsync<TEvent>(TEvent @event) where TEvent : IFluentEvent;
}