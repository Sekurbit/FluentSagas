using Microsoft.Extensions.DependencyInjection;

namespace SagaTest;

public class InstanceHelper
{
    public static object Create(IServiceProvider serviceProvider, Type type)
    {
        var constructorParameters = ResolveConstructorTypes(serviceProvider, type);
        return Activator.CreateInstance(type, constructorParameters)!;
    }
    
    public static T Create<T>(IServiceProvider serviceProvider)
    {
        var constructorParameterInstances = ResolveConstructorTypes(serviceProvider, typeof(T));
        return (T) Activator.CreateInstance(typeof(T), constructorParameterInstances)!;
    }
    
    public static object[] ResolveConstructorTypes(IServiceProvider serviceProvider, Type type)
    {
        var constructors = type.GetConstructors();
        if (!constructors.Any()) throw new InvalidOperationException($"No constructor available");

        var constructor = constructors.First();
        var parameterTypes = constructor.GetParameters();

        var instances = new List<object>();
        
        foreach (var parameterType in parameterTypes)
            instances.Add(serviceProvider.GetService(parameterType.ParameterType)!);

        return instances.ToArray();
    }
}