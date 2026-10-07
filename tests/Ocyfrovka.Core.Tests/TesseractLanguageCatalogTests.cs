using Ocyfrovka.Ocr.Tesseract;

namespace Ocyfrovka.Core.Tests;

public sealed class TesseractLanguageCatalogTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-tests",
        Guid.NewGuid().ToString("N"));

    public TesseractLanguageCatalogTests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "ukr.traineddata"), string.Empty);
        File.WriteAllText(Path.Combine(_directory, "eng.traineddata"), string.Empty);
        File.WriteAllText(Path.Combine(_directory, "osd.traineddata"), string.Empty);
    }

    [Fact]
    public void GetInstalledLanguages_ExcludesOsdAndReturnsInstalledModels()
    {
        var result = TesseractLanguageCatalog.GetInstalledLanguages(_directory);

        Assert.Equal(["eng", "ukr"], result.Select(x => x.Code).ToArray());
    }

    [Fact]
    public void GetMissingLanguages_HandlesMultiLanguageExpression()
    {
        var missing = TesseractLanguageCatalog.GetMissingLanguages(_directory, "ukr+eng+pol");

        Assert.Equal(["pol"], missing);
    }

    [Theory]
    [InlineData("ukr+eng", new[] { "ukr", "eng" })]
    [InlineData(" ukr + eng + ukr ", new[] { "ukr", "eng" })]
    public void ParseExpression_NormalizesLanguageList(string expression, string[] expected)
    {
        Assert.Equal(expected, TesseractLanguageCatalog.ParseExpression(expression));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
