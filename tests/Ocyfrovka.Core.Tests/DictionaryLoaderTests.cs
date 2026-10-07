using Ocyfrovka.Dictionary;

namespace Ocyfrovka.Core.Tests;

public sealed class DictionaryLoaderTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-dictionary-tests",
        Guid.NewGuid().ToString("N"));

    public DictionaryLoaderTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void MissingDictionary_ReturnsEmptyCatalog()
    {
        var catalog = DictionaryLoader.Load(
            Path.Combine(_root, "missing.json"));

        Assert.Empty(catalog.Entries);
    }

    [Fact]
    public void ValidDictionary_LoadsCanonicalAliasesAndCategory()
    {
        var path = Path.Combine(_root, "dictionary.json");
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 1,
              "entries": [
                {
                  "canonical": "Widget Alpha",
                  "aliases": ["Widget A", "W. Alpha"],
                  "category": "sample"
                }
              ]
            }
            """);

        var catalog = DictionaryLoader.Load(path);

        var entry = Assert.Single(catalog.Entries);
        Assert.Equal("Widget Alpha", entry.Canonical);
        Assert.Equal(2, entry.Aliases.Count);
        Assert.Equal("sample", entry.Category);
    }

    [Fact]
    public void UnsupportedSchema_IsRejected()
    {
        var path = Path.Combine(_root, "dictionary.json");
        File.WriteAllText(
            path,
            """
            {
              "schemaVersion": 99,
              "entries": []
            }
            """);

        Assert.Throws<InvalidDataException>(
            () => DictionaryLoader.Load(path));
    }

    [Fact]
    public void EnsureEmptyDictionary_IsNonDestructive()
    {
        var path = Path.Combine(_root, "dictionary.json");

        DictionaryLoader.EnsureEmptyDictionary(path);
        var first = File.ReadAllText(path);

        File.WriteAllText(path, first + Environment.NewLine);
        DictionaryLoader.EnsureEmptyDictionary(path);

        Assert.Equal(
            first + Environment.NewLine,
            File.ReadAllText(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
