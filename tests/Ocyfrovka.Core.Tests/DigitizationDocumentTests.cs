using Ocyfrovka.Core.Documents;

namespace Ocyfrovka.Core.Tests;

public sealed class DigitizationDocumentTests
{
    [Fact]
    public void AddPage_DoesNotDuplicateSameSourceFrame()
    {
        var document = new DigitizationDocument();
        var first = Page("C:\\docs\\a.tif", 0);
        var duplicate = Page("c:\\DOCS\\a.tif", 0);

        Assert.True(document.AddPage(first));
        Assert.False(document.AddPage(duplicate));
        Assert.Single(document.Pages);
    }

    [Fact]
    public void MoveUpAndDown_ReordersPagesWithoutChangingSource()
    {
        var document = new DigitizationDocument();
        var first = Page("a.jpg");
        var second = Page("b.jpg");
        var third = Page("c.jpg");
        document.AddPages([first, second, third]);

        Assert.True(document.MoveUp(third.Id));
        Assert.Equal([first.Id, third.Id, second.Id], document.Pages.Select(x => x.Id).ToArray());

        Assert.True(document.MoveDown(first.Id));
        Assert.Equal([third.Id, first.Id, second.Id], document.Pages.Select(x => x.Id).ToArray());
    }

    [Fact]
    public void Rotation_IsNonDestructiveMetadataAndWrapsAt360()
    {
        var page = Page("a.jpg");
        var source = page.SourcePath;

        page.RotateCounterClockwise();
        Assert.Equal(270, page.RotationDegrees);

        page.RotateClockwise();
        Assert.Equal(0, page.RotationDegrees);
        Assert.Equal(source, page.SourcePath);

        for (var i = 0; i < 5; i++)
        {
            page.RotateClockwise();
        }

        Assert.Equal(90, page.RotationDegrees);
    }

    [Fact]
    public void Remove_DeletesOnlyRequestedPage()
    {
        var document = new DigitizationDocument();
        var first = Page("a.jpg");
        var second = Page("b.jpg");
        document.AddPages([first, second]);

        Assert.True(document.Remove(first.Id));
        Assert.Equal(second.Id, Assert.Single(document.Pages).Id);
        Assert.False(document.Remove(Guid.NewGuid()));
    }

    private static DocumentPage Page(string path, int frame = 0)
        => new(path, frame, 100, 200);
}
