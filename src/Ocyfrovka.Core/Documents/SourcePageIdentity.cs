using System.Security.Cryptography;
using System.Text;

namespace Ocyfrovka.Core.Documents;

public static class SourcePageIdentity
{
    public static Guid CreateDocumentId(string sourcePath)
        => CreateGuid(BuildFileIdentity(sourcePath));

    public static Guid CreatePageId(
        string sourcePath,
        int sourceFrameIndex)
    {
        if (sourceFrameIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceFrameIndex));
        }

        return CreateGuid(
            $"{BuildFileIdentity(sourcePath)}|frame:{sourceFrameIndex}");
    }

    private static string BuildFileIdentity(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException(
                "Source path is required.",
                nameof(sourcePath));
        }

        var fullPath = Path.GetFullPath(sourcePath);
        var file = new FileInfo(fullPath);

        if (!file.Exists)
        {
            throw new FileNotFoundException(
                "Source file was not found.",
                fullPath);
        }

        return string.Join(
            '|',
            fullPath.ToUpperInvariant(),
            file.Length,
            file.LastWriteTimeUtc.Ticks);
    }

    private static Guid CreateGuid(string identity)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(identity));

        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);

        // RFC 4122-compatible variant/version bits. The value is still
        // deterministic because only fixed hash bits are replaced.
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes);
    }
}
