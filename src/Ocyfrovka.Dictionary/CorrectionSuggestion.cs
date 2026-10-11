namespace Ocyfrovka.Dictionary;

public sealed record CorrectionSuggestion(
    string Input,
    string Canonical,
    double Score,
    CorrectionMatchKind MatchKind,
    bool IsAutoCorrectionSafe,
    string? Category = null);
