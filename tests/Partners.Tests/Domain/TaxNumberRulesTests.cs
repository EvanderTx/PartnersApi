using Partners.Domain.Partners;

namespace Partners.Tests.Domain;

public sealed class TaxNumberRulesTests
{
    [Theory]
    [InlineData("001.234.567", "001234567")]
    [InlineData("001 234 567", "001234567")]
    [InlineData("001-234-567", "001234567")]
    [InlineData(null, "")]
    public void Normalize_RemovesSpacesDotsAndHyphens(string? input, string expected) =>
        Assert.Equal(expected, TaxNumberRules.Normalize(input));

    [Theory]
    [InlineData("001234567", true)]
    [InlineData("12345678", false)]
    [InlineData("1234567890", false)]
    [InlineData("12345678A", false)]
    [InlineData("١٢٣٤٥٦٧٨٩", false)]
    [InlineData("", false)]
    public void IsValid_RequiresExactlyNineAsciiDigits(string normalized, bool expected) =>
        Assert.Equal(expected, TaxNumberRules.IsValid(normalized));
}
