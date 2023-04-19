using FluentSaga.Abstractions;

namespace FluentSaga.Transports;

public interface IFluentMessagePublisher
{
    Task Publish<TMessage>(string sagaId, Type sagaType, TMessage message) where TMessage : IFluentEvent;
}