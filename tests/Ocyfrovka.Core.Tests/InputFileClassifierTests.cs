using Ocyfrovka.Core.Input;

namespace Ocyfrovka.Core.Tests;

public sealed class InputFileClassifierTests
{
    [Theory]
    [InlineData("scan.jpg")]
    [InlineData("scan.JPEG")]
    [InlineData("scan.png")]
    [InlineData("scan.bmp")]
    [InlineData("scan.tif")]
    [InlineData("scan.TIFF")]
    public void GetKind_RecognizesSupportedImages(string path)
    {
        Assert.Equal(InputFileKind.Image, InputFileClassifier.GetKind(path));
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("DOCUMENT.PDF")]
    public void GetKind_RecognizesPdf(string path)
    {
        Assert.Equal(InputFileKind.Pdf, InputFileClassifier.GetKind(path));
    }

    [Theory]
    [InlineData("archive.zip")]
    [InlineData("note.txt")]
    [InlineData("")]
    [InlineData(null)]
    public void GetKind_RejectsUnsupportedInput(string? path)
    {
        Assert.Equal(InputFileKind.Unsupported, InputFileClassifier.GetKind(path));
    }
}
