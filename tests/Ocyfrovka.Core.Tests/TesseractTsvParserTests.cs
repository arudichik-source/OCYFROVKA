using Ocyfrovka.Ocr.Tesseract;

namespace Ocyfrovka.Core.Tests;

public sealed class TesseractTsvParserTests
{
    [Fact]
    public void ParseTsv_PreservesLinesCoordinatesAndAverageConfidence()
    {
        var tsv = string.Join(
            "\n",
            "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext",
            "5\t1\t1\t1\t1\t1\t10\t20\t30\t12\t90.0\tHello",
            "5\t1\t1\t1\t1\t2\t45\t20\t25\t12\t80.0\tworld",
            "5\t1\t1\t1\t2\t1\t10\t40\t40\t12\t70.0\tSecond");

        var result = TesseractOcrEngine.ParseTsv(tsv);

        Assert.Equal($"Hello world{Environment.NewLine}Second", result.Text);
        Assert.Equal(80.0, result.Confidence, precision: 3);
        Assert.Equal(3, result.Words.Count);

        Assert.Equal("Hello", result.Words[0].Text);
        Assert.Equal(10, result.Words[0].X);
        Assert.Equal(20, result.Words[0].Y);
        Assert.Equal(30, result.Words[0].Width);
        Assert.Equal(12, result.Words[0].Height);
    }

    [Fact]
    public void ParseTsv_IgnoresNonWordRowsAndNegativeConfidenceInAverage()
    {
        var tsv = string.Join(
            "\n",
            "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext",
            "1\t1\t0\t0\t0\t0\t0\t0\t100\t100\t-1\t",
            "5\t1\t1\t1\t1\t1\t1\t2\t3\t4\t-1\tUnknown",
            "5\t1\t1\t1\t1\t2\t5\t2\t3\t4\t95.5\tKnown");

        var result = TesseractOcrEngine.ParseTsv(tsv);

        Assert.Equal("Unknown Known", result.Text);
        Assert.Equal(95.5, result.Confidence, precision: 3);
        Assert.Equal(2, result.Words.Count);
    }
}
