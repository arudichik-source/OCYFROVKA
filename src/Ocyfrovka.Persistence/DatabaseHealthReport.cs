namespace Ocyfrovka.Persistence;

public sealed record DatabaseHealthReport(
    bool IsHealthy,
    int SchemaVersion,
    string IntegrityResult,
    string DatabasePath);
