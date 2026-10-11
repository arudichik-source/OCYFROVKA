namespace Ocyfrovka.Export;

public sealed record ExportOptions(
    bool RequireReviewed = true,
    bool IncludeMetadata = false,
    string DataSheetName = "Дані",
    string MetadataSheetName = "Метадані");
