using System.Text.Json;

namespace Ocyfrovka.Dictionary;

public static class DictionaryLoader
{
    private sealed class DictionaryFileModel
    {
        public int SchemaVersion { get; set; } = 1;

        public List<DictionaryEntryModel> Entries { get; set; } = [];
    }

    private sealed class DictionaryEntryModel
    {
        public string Canonical { get; set; } = string.Empty;

        public List<string> Aliases { get; set; } = [];

        public string? Category { get; set; }
    }

    public static DictionaryCatalog Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Dictionary path is required.", nameof(path));
        }

        if (!File.Exists(path))
        {
            return DictionaryCatalog.Empty;
        }

        var json = File.ReadAllText(path);
        var model = JsonSerializer.Deserialize<DictionaryFileModel>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidDataException("Dictionary JSON is empty.");

        if (model.SchemaVersion != 1)
        {
            throw new InvalidDataException(
                $"Unsupported dictionary schema version: {model.SchemaVersion}.");
        }

        return new DictionaryCatalog(
            model.Entries.Select(entry => new DictionaryEntry(
                entry.Canonical,
                entry.Aliases,
                entry.Category)));
    }

    public static void EnsureEmptyDictionary(string path)
    {
        if (File.Exists(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var model = new DictionaryFileModel();
        var json = JsonSerializer.Serialize(
            model,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(path, json);
    }
}
