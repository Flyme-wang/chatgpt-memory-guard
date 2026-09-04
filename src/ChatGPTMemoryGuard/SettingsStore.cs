using System.Text.Json;

namespace ChatGPTMemoryGuard;

public sealed record SettingsLoadResult(GuardSettings Settings, bool UsedDefaults, Exception? Error);

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    public SettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = Path.GetFullPath(settingsPath);
    }

    public SettingsLoadResult Load()
    {
        if (!File.Exists(_settingsPath))
        {
            Save(GuardSettings.Default);
            return new SettingsLoadResult(GuardSettings.Default, true, null);
        }

        try
        {
            string json = File.ReadAllText(_settingsPath);
            GuardSettings settings = JsonSerializer.Deserialize<GuardSettings>(json, JsonOptions)
                ?? throw new JsonException("配置文件内容为空。");
            return new SettingsLoadResult(settings.Validate(), false, null);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            PreserveCorruptFile();
            Save(GuardSettings.Default);
            return new SettingsLoadResult(GuardSettings.Default, true, error);
        }
    }

    public void Save(GuardSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string? directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(settings.Validate(), JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private void PreserveCorruptFile()
    {
        string? directory = Path.GetDirectoryName(_settingsPath);
        string fileName = $"settings.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}.json";
        string backupPath = Path.Combine(directory ?? string.Empty, fileName);
        File.Move(_settingsPath, backupPath);
    }
}
