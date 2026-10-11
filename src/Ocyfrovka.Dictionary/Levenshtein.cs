namespace Ocyfrovka.Dictionary;

public static class Levenshtein
{
    public static int Distance(string left, string right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Length == 0)
        {
            return right.Length;
        }

        if (right.Length == 0)
        {
            return left.Length;
        }

        if (left.Length > right.Length)
        {
            (left, right) = (right, left);
        }

        var previous = new int[left.Length + 1];
        var current = new int[left.Length + 1];

        for (var i = 0; i <= left.Length; i++)
        {
            previous[i] = i;
        }

        for (var row = 1; row <= right.Length; row++)
        {
            current[0] = row;

            for (var column = 1; column <= left.Length; column++)
            {
                var cost = left[column - 1] == right[row - 1] ? 0 : 1;

                current[column] = Math.Min(
                    Math.Min(
                        current[column - 1] + 1,
                        previous[column] + 1),
                    previous[column - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[left.Length];
    }

    public static double Similarity(string left, string right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var length = Math.Max(left.Length, right.Length);
        if (length == 0)
        {
            return 1;
        }

        return 1.0 - Distance(left, right) / (double)length;
    }
}
