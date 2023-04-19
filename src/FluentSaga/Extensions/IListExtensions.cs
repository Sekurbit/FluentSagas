using FluentSaga;

namespace SagaTest.Extensions;

public static class IListExtensions
{
    public static T? Get<T>(this IList<FluentSagaStep> steps) where T : FluentSagaStep
    {
        return (T?)steps.FirstOrDefault(x => x.GetType() == typeof(T));
    }
}