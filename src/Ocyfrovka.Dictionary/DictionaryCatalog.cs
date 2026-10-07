namespace Ocyfrovka.Dictionary;

public sealed class DictionaryCatalog
{
    private readonly IReadOnlyList<DictionaryEntry> _entries;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DictionaryEntry>> _exactIndex;

    public DictionaryCatalog(IEnumerable<DictionaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        _entries = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Canonical))
            .Select(entry => new DictionaryEntry(
                entry.Canonical.Trim(),
                (entry.Aliases ?? [])
                    .Where(alias => !string.IsNullOrWhiteSpace(alias))
                    .Select(alias => alias.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                string.IsNullOrWhiteSpace(entry.Category)
                    ? null
                    : entry.Category.Trim()))
            .ToArray();

        var index = new Dictionary<string, List<DictionaryEntry>>(
            StringComparer.Ordinal);

        foreach (var entry in _entries)
        {
            foreach (var form in GetNormalizedForms(entry))
            {
                if (!index.TryGetValue(form, out var bucket))
                {
                    bucket = [];
                    index[form] = bucket;
                }

                if (!bucket.Any(existing =>
                        string.Equals(
                            existing.Canonical,
                            entry.Canonical,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    bucket.Add(entry);
                }
            }
        }

        _exactIndex = index.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<DictionaryEntry>)pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    public static DictionaryCatalog Empty { get; } = new([]);

    public IReadOnlyList<DictionaryEntry> Entries => _entries;

    public IReadOnlyList<DictionaryEntry> FindExact(
        string normalizedValue)
    {
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return [];
        }

        return _exactIndex.TryGetValue(normalizedValue, out var entries)
            ? entries
            : [];
    }

    internal IReadOnlyList<string> GetNormalizedForms(
        DictionaryEntry entry)
        => new[] { entry.Canonical }
            .Concat(entry.Aliases ?? [])
            .Select(value => CorrectionNormalizer.NormalizeComparable(
                value,
                CorrectionFieldKind.Nomenclature))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
