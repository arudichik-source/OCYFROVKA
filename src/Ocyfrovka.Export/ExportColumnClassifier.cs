namespace Ocyfrovka.Export;

public static class ExportColumnClassifier
{
    private static readonly string[] TextIdentifiers =
    [
        "код",
        "номер",
        "артикул",
        "парті",
        "серійн",
        "інвентарн",
        "ідентифікатор",
        "реєстр",
        "nsn",
        "sku",
        "serial",
        "article",
        "code",
        "barcode",
        "identifier"
    ];

    private static readonly string[] NumericIndicators =
    [
        "кількіст",
        "кільк.",
        "к-сть",
        "ціна",
        "сум",
        "вартіст",
        "залишок",
        "залишків",
        "маса",
        "вага",
        "обсяг",
        "qty",
        "quantity",
        "amount",
        "price",
        "cost",
        "total",
        "weight",
        "volume",
        "balance"
    ];

    public static bool IsNumericHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return false;
        }

        var name = header.Trim().ToLowerInvariant();

        if (TextIdentifiers.Any(name.Contains))
        {
            return false;
        }

        return NumericIndicators.Any(name.Contains);
    }
}
