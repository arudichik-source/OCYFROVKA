using Ocyfrovka.Core.Documents;

namespace Ocyfrovka.Core.Tests;

public sealed class DocumentPageTests
{
    [Fact]
    public void PdfPage_UsesRenderedImageForPreviewAndKeepsOriginalSource()
    {
        var page = new DocumentPage(
            sourcePath: "document.pdf",
            sourceFrameIndex: 2,
            pixelWidth: 1600,
            pixelHeight: 2200,
            renderedImagePath: "Workspace/Temp/PdfPages/x/page-0003.png");

        Assert.True(page.IsPdfPage);
        Assert.Equal("document.pdf", page.SourcePath);
        Assert.Equal("Workspace/Temp/PdfPages/x/page-0003.png", page.ImagePath);
        Assert.Equal("document.pdf — стор. 3", page.DisplayName);
    }

    [Fact]
    public void ImagePage_UsesOriginalImageAsPreviewSource()
    {
        var page = new DocumentPage("scan.png", 0, 1000, 1400);

        Assert.False(page.IsPdfPage);
        Assert.Equal("scan.png", page.ImagePath);
        Assert.Equal("scan.png", page.DisplayName);
    }

    [Fact]
    public void MultiFrameImage_ShowsFrameNumber()
    {
        var page = new DocumentPage("scan.tiff", 1, 1000, 1400);

        Assert.Equal("scan.tiff — кадр 2", page.DisplayName);
    }
}
