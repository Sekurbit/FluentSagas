namespace FluentSaga.Abstractions;

public interface IFluentCorrelationStore
{
    /// <summary>
    /// Gets or sets the correlation ID of the saga
    /// </summary>
    string CorrelationId { get; set; }
}