using Microsoft.Extensions.DependencyInjection;

namespace SagaTest;

public class InstanceHelper
{
    public static object Create(IServiceProvider serviceProvider, Type type)
    {
        // We need to create a new scope from the Service Provider here to be able to resolve services that
        // are not singletons. These could for eg. be contexts for EF Core, services etc.
        using var serviceScope = serviceProvider.CreateScope();
        
        var constructorParameters = ResolveConstructorTypes(serviceScope.ServiceProvider, type);
        return Activator.CreateInstance(type, constructorParameters)!;
    }
    
    public static T Create<T>(IServiceProvider serviceProvider)
    {
        // We need to create a new scope from the Service Provider here to be able to resolve services that
        // are not singletons. These could for eg. be contexts for EF Core, services etc.
        using var serviceScope = serviceProvider.CreateScope();
        
        var constructorParameterInstances = ResolveConstructorTypes(serviceScope.ServiceProvider, typeof(T));
        return (T) Activator.CreateInstance(typeof(T), constructorParameterInstances)!;
    }
    
    public static object[] ResolveConstructorTypes(IServiceProvider serviceProvider, Type type)
    {
        // We need to create a new scope from the Service Provider here to be able to resolve services that
        // are not singletons. These could for eg. be contexts for EF Core, services etc.
        using var serviceScope = serviceProvider.CreateScope();
        
        var constructors = type.GetConstructors();
        if (!constructors.Any()) throw new InvalidOperationException($"No constructor available");

        var constructor = constructors.First();
        var parameterTypes = constructor.GetParameters();

        var instances = new List<object>();
        
        foreach (var parameterType in parameterTypes)
            instances.Add(serviceScope.ServiceProvider.GetService(parameterType.ParameterType)!);

        return instances.ToArray();
    }
}