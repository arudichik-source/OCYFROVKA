namespace Ocyfrovka.Core.Documents;

public sealed class DigitizationDocument
{
    private readonly List<DocumentPage> _pages = [];

    public IReadOnlyList<DocumentPage> Pages => _pages;

    public int Count => _pages.Count;

    public bool AddPage(DocumentPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (_pages.Any(existing =>
                existing.SourceFrameIndex == page.SourceFrameIndex &&
                string.Equals(existing.SourcePath, page.SourcePath, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _pages.Add(page);
        return true;
    }

    public int AddPages(IEnumerable<DocumentPage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        var added = 0;
        foreach (var page in pages)
        {
            if (AddPage(page))
            {
                added++;
            }
        }

        return added;
    }

    public bool Remove(Guid pageId)
    {
        var index = IndexOf(pageId);
        if (index < 0)
        {
            return false;
        }

        _pages.RemoveAt(index);
        return true;
    }

    public bool MoveUp(Guid pageId)
    {
        var index = IndexOf(pageId);
        if (index <= 0)
        {
            return false;
        }

        (_pages[index - 1], _pages[index]) = (_pages[index], _pages[index - 1]);
        return true;
    }

    public bool MoveDown(Guid pageId)
    {
        var index = IndexOf(pageId);
        if (index < 0 || index >= _pages.Count - 1)
        {
            return false;
        }

        (_pages[index + 1], _pages[index]) = (_pages[index], _pages[index + 1]);
        return true;
    }

    public void Clear() => _pages.Clear();

    private int IndexOf(Guid pageId) => _pages.FindIndex(page => page.Id == pageId);
}
