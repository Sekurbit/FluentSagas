namespace FluentSaga.Abstractions;

public interface IFluentEvent
{
    /// <summary>
    /// Gets or sets the message ID
    /// </summary>
    string Id { get; set; }
    
    /// <summary>
    /// Gets or sets the correlation ID which can be used to track multiple events together as one flow
    /// </summary>
    string CorrelationId { get; set; }
    
    /// <summary>
    /// Gets or sets the saga ID
    /// </summary>
    string SagaId { get; set; }
}