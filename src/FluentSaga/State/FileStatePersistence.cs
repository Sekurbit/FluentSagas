using System.Text;
using System.Text.Json;

namespace FluentSaga.State;

public class FileStatePersistence : IFluentSagaStatePersistence
{
    private const string StateFileName = "state_{0}.json";
    private const string FolderName = "FluentSagas";
    
    public async Task<TStateType?> LoadAsync<TStateType>(string sagaId) where TStateType : IFluentSagaState
    {
        var filePath = GetPath(sagaId);
        if (!File.Exists(filePath)) return default;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<TStateType>(json);
    }

    public async Task<object?> LoadAsync(Type sagaStateType, string sagaId)
    {
        var filePath = GetPath(sagaId);
        if (!File.Exists(filePath)) return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize(json, sagaStateType);
    }

    public async Task SaveAsync(string sagaId, object state)
    {
        var filePath = GetPath(sagaId);
        var json = JsonSerializer.Serialize(state);
        
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }

    public async Task CompleteAsync(string sagaId)
    {
        var filePath = GetPath(sagaId);
        if (!File.Exists(filePath)) return;
        
        File.Delete(filePath);
    }

    private string GetPath(string sagaId)
    {
        var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            FolderName);

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        return Path.Combine(folderPath, string.Format(StateFileName, sagaId));
    }
}