namespace OneST;

public static class UenValidator
{
    private static readonly HashSet<string> EntityTypes = new(StringComparer.Ordinal)
    {
        "LP", "LL", "FC", "PF", 
        "RF", 
        "MQ", "MM", "NB", "CC", "CS", "MB", "FM", "GS",
        "DP", "CP", "NR", 
        "CM", "CD", "MD", "HS", "VH", "CH", "MH", "CL", "XL", "CX", "HC", 
        "RP", "TU", "TC", "FB", "FN", "PA", "PB", "SS", "MC", "SM", "GA", "GB"
    };

    public static bool IsValid(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var uen = input.Trim();
        if (uen.Length is not (9 or 10))
            return false;

        foreach (var character in uen)
        {
            if (!char.IsAsciiLetterOrDigit(character))
                return false;
        }

        uen = uen.ToUpperInvariant();
        if (!char.IsAsciiLetter(uen[^1]))
            return false;

        if (uen.Length == 9)
            return IsDigits(uen.AsSpan(0, 8));

        if (IsDigits(uen.AsSpan(0, 9)))
            return true;

        return uen[0] is ('R' or 'S' or 'T')
            && IsDigits(uen.AsSpan(1, 2))
            && EntityTypes.Contains(uen.Substring(3, 2))
            && IsDigits(uen.AsSpan(5, 4));
    }

    private static bool IsDigits(ReadOnlySpan<char> value)
    {
        foreach (var character in value)
        {
            if (!char.IsAsciiDigit(character))
                return false;
        }
        return true;
    }
}