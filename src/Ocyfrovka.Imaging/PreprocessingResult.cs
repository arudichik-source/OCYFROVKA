namespace Ocyfrovka.Imaging;

public sealed record PreprocessingResult(
    GrayImage Image,
    ImageQualityReport Quality,
    int OtsuThreshold,
    PreprocessingProfile Profile);
