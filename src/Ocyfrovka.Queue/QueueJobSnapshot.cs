namespace Ocyfrovka.Queue;

public sealed record QueueJobSnapshot(
    Guid Id,
    string DisplayName,
    QueueJobStatus Status,
    int Attempts,
    string? Error);
