using FluentSaga.Transports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSaga.Web;

public static class IApplicationBuilderExtensions
{
    public static WebApplication UseFluentSagas(this WebApplication app)
    {
        var router = app.Services.GetService<IFluentSagaRouter>();

        if (router == null)
            throw new InvalidOperationException("builder.Services.AddFluentSagas() needs to be called before this.");

        var messagePublisher = app.Services.GetService<IFluentMessagePublisher>();
        if (messagePublisher == null)
            throw new InvalidOperationException(
                "No IMessagePublisher has been registered. Please register one in the DI container.");
        
        var task = router.InitializeAsync();
        task.Wait();
        
        return app;
    }
}