namespace FluentSaga.State;

public interface IFluentSagaState
{
    string SagaId { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if the saga has completed
    /// </summary>
    bool Completed { get; set; }
}

public class FluentSagaState : IFluentSagaState
{
    public string SagaId { get; set; }
    public bool Completed { get; set; }
}