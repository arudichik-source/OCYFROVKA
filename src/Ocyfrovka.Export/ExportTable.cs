using Ocyfrovka.Review;

namespace Ocyfrovka.Export;

public sealed record ExportTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<ExportRow> Rows,
    ReviewTableSession SourceSession);
