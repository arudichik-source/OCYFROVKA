namespace Ocyfrovka.Dictionary;

public sealed record NumericCorrectionResult(
    string Original,
    string Corrected,
    bool Changed);
