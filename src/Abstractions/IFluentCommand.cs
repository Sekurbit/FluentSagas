namespace FluentSaga.Abstractions;

public interface IFluentCommand
{
    /// <summary>
    /// Command that performs one or more tasks during the saga execution
    /// </summary>
    /// <param name="event"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task ExecuteAsync<TEvent>(TEvent @event) where TEvent : IFluentEvent;
}