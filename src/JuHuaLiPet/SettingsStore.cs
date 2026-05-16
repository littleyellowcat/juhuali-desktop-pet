using System.IO;
using System.Text.Json;

namespace JuHuaLiPet;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public SettingsStore()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JuHuaLiPet");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "settings.json");
        Settings = Load();
        Settings.Clamp();
    }

    public PetSettings Settings { get; }

    public void Save()
    {
        Settings.Clamp();
        var json = JsonSerializer.Serialize(Settings, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private PetSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return new PetSettings();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<PetSettings>(json, JsonOptions) ?? new PetSettings();
        }
        catch
        {
            return new PetSettings();
        }
    }
}
