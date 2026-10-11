using Ocyfrovka.Core.Documents;
using Ocyfrovka.Persistence;
using Ocyfrovka.Review;

namespace Ocyfrovka.App.Services;

internal sealed record RestoredPageState(
    string OcrText,
    ReviewTableSession? ReviewSession,
    int RotationDegrees,
    DateTimeOffset UpdatedUtc);

internal sealed class ReviewPersistenceService
{
    private readonly OcyfrovkaDatabase _database;

    public ReviewPersistenceService(string applicationBaseDirectory)
    {
        _database = new OcyfrovkaDatabase(
            OcyfrovkaDatabase.GetDefaultPath(
                applicationBaseDirectory));

        _database.Initialize();
    }

    public string DatabasePath => _database.DatabasePath;

    public RestoredPageState? Restore(DocumentPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var persisted = _database.LoadPageReview(page.Id);
        if (persisted is null)
        {
            return null;
        }

        return new RestoredPageState(
            persisted.OcrText,
            persisted.Review is null
                ? null
                : ReviewTableSession.FromSnapshot(persisted.Review),
            persisted.RotationDegrees,
            persisted.UpdatedUtc);
    }

    public void Save(
        DocumentPage page,
        string? ocrText,
        ReviewTableSession? reviewSession)
    {
        ArgumentNullException.ThrowIfNull(page);

        _database.SavePageReview(
            new PersistedPageReview(
                SourcePageIdentity.CreateDocumentId(page.SourcePath),
                page.Id,
                page.SourcePath,
                page.SourceFrameIndex,
                page.RotationDegrees,
                ocrText ?? string.Empty,
                reviewSession?.ToSnapshot(),
                DateTimeOffset.UtcNow));
    }

    public void RememberCorrection(
        string normalizedInput,
        string canonical,
        string fieldKind)
        => _database.RecordCorrection(
            normalizedInput,
            canonical,
            fieldKind);

    public IReadOnlyList<CorrectionMemoryEntry> FindCorrections(
        string normalizedInput,
        string fieldKind,
        int limit = 5)
        => _database.FindCorrections(
            normalizedInput,
            fieldKind,
            limit);
}
