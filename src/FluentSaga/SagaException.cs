namespace FluentSaga;

public class SagaException : Exception
{
    public SagaException(string message, Exception? inner = null)
        : base(message, inner)
    {
        
    }
}