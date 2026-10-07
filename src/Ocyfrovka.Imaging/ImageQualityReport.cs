namespace Ocyfrovka.Imaging;

public sealed record ImageQualityReport(
    double Brightness,
    double Contrast,
    double EdgeScore,
    double OverallScore,
    IReadOnlyList<string> Warnings);
