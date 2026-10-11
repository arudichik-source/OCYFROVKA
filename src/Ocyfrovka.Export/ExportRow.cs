namespace Ocyfrovka.Export;

public sealed record ExportRow(
    int SourceRowIndex,
    IReadOnlyList<string> Values);
