using Ocyfrovka.Export;

namespace Ocyfrovka.Core.Tests;

public sealed class ExportColumnClassifierTests
{
    [Theory]
    [InlineData("Кількість")]
    [InlineData("Ціна за одиницю")]
    [InlineData("Сума, грн")]
    [InlineData("Залишок, шт")]
    [InlineData("Quantity")]
    [InlineData("Total")]
    public void NumericMeasureHeaders_AreNumeric(string header)
    {
        Assert.True(ExportColumnClassifier.IsNumericHeader(header));
    }

    [Theory]
    [InlineData("Код")]
    [InlineData("Код 123")]
    [InlineData("Інвентарний номер")]
    [InlineData("№ з/п")]
    [InlineData("Артикул")]
    [InlineData("NSN")]
    [InlineData("Колонка 2")]
    [InlineData("")]
    public void IdentityAndUnknownHeaders_AreAlwaysText(string header)
    {
        Assert.False(ExportColumnClassifier.IsNumericHeader(header));
    }
}
