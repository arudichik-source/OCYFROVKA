namespace Ocyfrovka.Dictionary;

public sealed class DictionaryCatalog
{
    private readonly IReadOnlyList<DictionaryEntry> _entries;

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

        return _entries
            .Where(entry =>
                AllNormalizedForms(entry)
                    .Contains(normalizedValue, StringComparer.Ordinal))
            .GroupBy(
                entry => CorrectionNormalizer.NormalizeComparable(
                    entry.Canonical,
                    CorrectionFieldKind.Nomenclature),
                StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
    }

    internal IEnumerable<(DictionaryEntry Entry, string Form)> EnumerateForms()
    {
        foreach (var entry in _entries)
        {
            foreach (var form in AllNormalizedForms(entry))
            {
                yield return (entry, form);
            }
        }
    }

    private static IReadOnlyList<string> AllNormalizedForms(
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
