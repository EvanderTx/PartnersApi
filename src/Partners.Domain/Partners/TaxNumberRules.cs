namespace Partners.Domain.Partners;

public static class TaxNumberRules
{
    public const int Length = 9;

    public static string Normalize(string? value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : string.Concat(value.Where(c => c is not (' ' or '.' or '-')));

    public static bool IsValid(string normalized) =>
        normalized.Length == Length && normalized.All(char.IsAsciiDigit);
}