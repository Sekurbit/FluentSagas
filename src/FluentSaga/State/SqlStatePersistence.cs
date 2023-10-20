using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace FluentSaga.State;

public class SqlStatePersistence : IFluentSagaStatePersistence
{
    private readonly SqlStateContext _context;

    public SqlStatePersistence(SqlStateContext context)
    {
        _context = context;
    }
    
    public async Task<TStateType?> LoadAsync<TStateType>(string sagaId) where TStateType : IFluentSagaState
    {
        var state = await _context.States.FirstOrDefaultAsync(x => x.SagaId.Equals(sagaId));
        if (state == null) return default;
        
        return JsonSerializer.Deserialize<TStateType>(state.State);
    }

    public async Task<object?> LoadAsync(Type sagaStateType, string sagaId)
    {
        var state = await _context.States.FirstOrDefaultAsync(x => x.SagaId.Equals(sagaId));
        if (state == null) return default;
        
        return JsonSerializer.Deserialize<dynamic>(state.State);
    }

    public async Task SaveAsync(string sagaId, object state)
    {
        var stateJson = JsonSerializer.Serialize(state);

        var existing = await _context.States.FirstOrDefaultAsync(x => x.SagaId.Equals(sagaId));
        if (existing == null)
        {
            _context.States.Add(new SqlSagaState
            {
                State = stateJson,
                SagaId = sagaId
            });
        }
        else
        {
            existing.State = stateJson;
        }

        await _context.SaveChangesAsync();
    }

    public async Task CompleteAsync(string sagaId)
    {
        var existing = await _context.States.FirstOrDefaultAsync(x => x.SagaId.Equals(sagaId));
        if (existing == null) return;
        
        _context.States.Remove(existing);
        await _context.SaveChangesAsync();
    }
}