namespace Ocyfrovka.Dictionary;

public sealed record DictionaryEntry(
    string Canonical,
    IReadOnlyList<string> Aliases,
    string? Category = null);
