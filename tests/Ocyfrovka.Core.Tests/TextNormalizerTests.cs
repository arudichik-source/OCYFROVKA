using Ocyfrovka.Core.Text;

namespace Ocyfrovka.Core.Tests;

public sealed class TextNormalizerTests
{
    [Theory]
    [InlineData("  5.45   мм  патрон ", "5,45 мм патрон")]
    [InlineData("5,45  мм", "5,45 мм")]
    [InlineData(null, "")]
    public void NormalizeName_ReturnsExpectedValue(string? input, string expected)
    {
        Assert.Equal(expected, TextNormalizer.NormalizeName(input));
    }
}