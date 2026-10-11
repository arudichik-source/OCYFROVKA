using Ocyfrovka.Core.Documents;

namespace Ocyfrovka.Core.Tests;

public sealed class SourcePageIdentityTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "ocyfrovka-source-id-tests",
        Guid.NewGuid().ToString("N"));

    public SourcePageIdentityTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void SameUnchangedFileAndFrame_HasStableIdentity()
    {
        var path = CreateFile("sample.dat", "alpha");

        var firstDocument = SourcePageIdentity.CreateDocumentId(path);
        var secondDocument = SourcePageIdentity.CreateDocumentId(path);
        var firstPage = SourcePageIdentity.CreatePageId(path, 2);
        var secondPage = SourcePageIdentity.CreatePageId(path, 2);

        Assert.Equal(firstDocument, secondDocument);
        Assert.Equal(firstPage, secondPage);
    }

    [Fact]
    public void DifferentFrames_HaveDifferentPageIdentity()
    {
        var path = CreateFile("multi.dat", "alpha");

        Assert.NotEqual(
            SourcePageIdentity.CreatePageId(path, 0),
            SourcePageIdentity.CreatePageId(path, 1));
    }

    [Fact]
    public void ChangedFile_HasDifferentIdentity()
    {
        var path = CreateFile("changed.dat", "first");

        var beforeDocument = SourcePageIdentity.CreateDocumentId(path);
        var beforePage = SourcePageIdentity.CreatePageId(path, 0);

        File.AppendAllText(path, "-changed");
        File.SetLastWriteTimeUtc(
            path,
            DateTime.UtcNow.AddSeconds(2));

        var afterDocument = SourcePageIdentity.CreateDocumentId(path);
        var afterPage = SourcePageIdentity.CreatePageId(path, 0);

        Assert.NotEqual(beforeDocument, afterDocument);
        Assert.NotEqual(beforePage, afterPage);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string CreateFile(string name, string content)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, content);
        return path;
    }
}
