using Ocyfrovka.Review;

namespace Ocyfrovka.Persistence;

public sealed record PersistedPageReview(
    Guid DocumentId,
    Guid PageId,
    string SourcePath,
    int SourceFrameIndex,
    int RotationDegrees,
    string OcrText,
    ReviewTableSnapshot? Review,
    DateTimeOffset UpdatedUtc);
