namespace Ocyfrovka.Core.Models;

public enum DocumentStatus
{
    New,
    Processing,
    NeedsReview,
    Reviewed,
    Exported,
    Error
}