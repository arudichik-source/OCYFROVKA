using Ocyfrovka.Export;

namespace Ocyfrovka.Core.Tests;

public sealed class ExportValueParserTests
{
    [Theory]
    [InlineData("123", 123)]
    [InlineData("-12,5", -12.5)]
    [InlineData("+0.25", 0.25)]
    public void SafeNumbers_AreExportedAsNumbers(
        string input,
        double expected)
    {
        var result = ExportValueParser.Parse(input);

        Assert.Equal(ExportValueKind.Number, result.Kind);
        Assert.Equal(expected, result.Number!.Value, precision: 8);
    }

    [Theory]
    [InlineData("00123")]
    [InlineData("ABC123")]
    [InlineData("2026-10-11")]
    [InlineData("12/34")]
    [InlineData("1234567890123456")]
    [InlineData("=1+1")]
    public void AmbiguousIdentifiers_RemainText(string input)
    {
        var result = ExportValueParser.Parse(input);

        Assert.Equal(ExportValueKind.Text, result.Kind);
        Assert.Equal(input, result.Text);
        Assert.Null(result.Number);
    }

    [Fact]
    public void BlankInput_ProducesEmptyValue()
    {
        var result = ExportValueParser.Parse("   ");

        Assert.Equal(ExportValueKind.Empty, result.Kind);
    }
}
