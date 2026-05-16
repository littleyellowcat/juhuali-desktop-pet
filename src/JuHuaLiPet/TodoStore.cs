using System.IO;
using System.Text.Json;

namespace JuHuaLiPet;

internal sealed class TodoStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public TodoStore()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JuHuaLiPet");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "todos.json");
        Items = Load();
    }

    public List<TodoItem> Items { get; }

    public void Add(TodoItem item)
    {
        Items.Add(item);
        Save();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Items, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public IReadOnlyList<TodoItem> DueItems()
    {
        var now = DateTime.Now;
        return Items
            .Where(item => !item.IsDone && !item.Notified && item.DueAt <= now)
            .OrderBy(item => item.DueAt)
            .ToList();
    }

    private List<TodoItem> Load()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<TodoItem>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
