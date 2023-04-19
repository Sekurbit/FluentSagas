using FluentSaga.Transports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FluentSaga.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseFluentSagas(this IApplicationBuilder app)
    {
        var router = app.ApplicationServices.GetService<IFluentSagaRouter>();

        if (router == null)
            throw new InvalidOperationException("builder.Services.AddFluentSagas() needs to be called before this.");

        var messagePublisher = app.ApplicationServices.GetService<IFluentMessagePublisher>();
        if (messagePublisher == null)
            throw new InvalidOperationException(
                "No IMessagePublisher has been registered. Please register one in the DI container.");
        
        var task = router.InitializeAsync();
        task.Wait();
        
        return app;
    }
    
    public static IHost UseFluentSagas(this IHost host)
    {
        var router = (IFluentSagaRouter)host.Services.GetService(typeof(IFluentSagaRouter))!;

        if (router == null)
            throw new InvalidOperationException("builder.Services.AddFluentSagas() needs to be called before this.");

        var task = router.InitializeAsync();
        task.Wait();
        
        return host;
    }
}