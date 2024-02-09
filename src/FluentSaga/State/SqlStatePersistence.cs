using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FluentSaga.State;

public class SqlStatePersistence : IFluentSagaStatePersistence
{
    private readonly IServiceProvider _serviceProvider;

    public SqlStatePersistence(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task<TStateType?> LoadAsync<TStateType>(string sagaId) where TStateType : IFluentSagaState
    {
        using var scope = _serviceProvider.CreateScope();
        await using var context = scope.ServiceProvider.GetService<SqlStateContext>();
        
        var state = await context.States.FirstOrDefaultAsync(x => x.SagaId.Equals(sagaId));
        if (state == null) return default;
        
        return JsonSerializer.Deserialize<TStateType>(state.State);
    }

    public async Task<object?> LoadAsync(Type sagaStateType, string sagaId)
    {
        using var scope = _serviceProvider.CreateScope();
        await using var context = scope.ServiceProvider.GetService<SqlStateContext>();
        
        var state = await context.States.FirstOrDefaultAsync(x => x.SagaId.Equals(sagaId));
        if (state == null) return default;
        
        return JsonSerializer.Deserialize<dynamic>(state.State);
    }

    public async Task SaveAsync(string sagaId, object state)
    {
        using var scope = _serviceProvider.CreateScope();
        await using var context = scope.ServiceProvider.GetService<SqlStateContext>();
        
        var stateJson = JsonSerializer.Serialize(state);

        var existing = await context.States.FirstOrDefaultAsync(x => x.SagaId.Equals(sagaId));
        if (existing == null)
        {
            context.States.Add(new SqlSagaState
            {
                State = stateJson,
                SagaId = sagaId
            });
        }
        else
        {
            existing.State = stateJson;
        }

        await context.SaveChangesAsync();
    }

    public async Task CompleteAsync(string sagaId)
    {
        using var scope = _serviceProvider.CreateScope();
        await using var context = scope.ServiceProvider.GetService<SqlStateContext>();
        
        var existing = await context.States.FirstOrDefaultAsync(x => x.SagaId.Equals(sagaId));
        if (existing == null) return;
        
        context.States.Remove(existing);
        await context.SaveChangesAsync();
    }
}