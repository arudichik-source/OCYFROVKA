namespace Ocyfrovka.Imaging;

public sealed record PreprocessingResult(
    GrayImage Image,
    ImageQualityReport Quality,
    int OtsuThreshold,
    double DeskewAngle,
    ImageRect ContentBounds,
    PreprocessingProfile Profile);
