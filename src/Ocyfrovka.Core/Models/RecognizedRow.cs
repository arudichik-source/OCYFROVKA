namespace Ocyfrovka.Core.Models;

public sealed record RecognizedRow(
    int? Number,
    string Name,
    string? Unit,
    decimal? Quantity,
    double Confidence);