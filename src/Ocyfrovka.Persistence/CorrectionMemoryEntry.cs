namespace Ocyfrovka.Persistence;

public sealed record CorrectionMemoryEntry(
    string InputNormalized,
    string Canonical,
    string FieldKind,
    int HitCount,
    DateTimeOffset LastUsedUtc);
