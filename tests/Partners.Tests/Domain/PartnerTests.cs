using Partners.Domain.Common;
using Partners.Domain.Partners;

namespace Partners.Tests.Domain;

public sealed class PartnerTests
{
    private static readonly DateTime CreatedAt = new(2026, 10, 13, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedAt = new(2026, 10, 13, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_NormalizesValuesAndStartsActive()
    {
        var partner = Partner.Create(" Ana Silva ", "001.234.567", PartnerType.Customer, "  ", 10.50m, CreatedAt);

        Assert.NotEqual(Guid.Empty, partner.Id);
        Assert.Equal("Ana Silva", partner.Name);
        Assert.Equal("001234567", partner.TaxNumber);
        Assert.Null(partner.Email);
        Assert.True(partner.IsActive);
        Assert.Equal(CreatedAt, partner.CreatedAt);
        Assert.Equal(CreatedAt, partner.UpdatedAt);
    }

    [Fact]
    public void Create_ReportsEveryInvalidField()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            Partner.Create("A", "12AB", (PartnerType)0, "invalid", 10.999m, CreatedAt));

        Assert.Equal(
            ["CreditLimit", "Email", "Name", "TaxNumber", "Type"],
            exception.Errors.Keys.Order());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10.99)]
    [InlineData(1000000)]
    public void Create_AcceptsCreditLimitsWithinRules(decimal creditLimit)
    {
        var partner = Partner.Create("Ana Silva", "123456789", PartnerType.Both, null, creditLimit, CreatedAt);

        Assert.Equal(creditLimit, partner.CreditLimit);
    }

    [Fact]
    public void Update_ChangesUpdatedAtButNotCreatedAt()
    {
        var partner = Partner.Create("Ana Silva", "123456789", PartnerType.Customer, null, 0m, CreatedAt);

        partner.Update("Ana Maria", "123456789", PartnerType.Both, "ana@example.com", 50m, false, UpdatedAt);

        Assert.Equal(CreatedAt, partner.CreatedAt);
        Assert.Equal(UpdatedAt, partner.UpdatedAt);
        Assert.False(partner.IsActive);
    }

    [Fact]
    public void Update_WhenInvalid_DoesNotChangeAnyField()
    {
        var partner = Partner.Create("Ana Silva", "123456789", PartnerType.Customer, null, 0m, CreatedAt);

        Assert.Throws<DomainValidationException>(() =>
            partner.Update("Nome Novo", "987654321", PartnerType.Both, "invalid", 10m, false, UpdatedAt));

        Assert.Equal("Ana Silva", partner.Name);
        Assert.Equal("123456789", partner.TaxNumber);
        Assert.Equal(PartnerType.Customer, partner.Type);
        Assert.True(partner.IsActive);
        Assert.Equal(CreatedAt, partner.UpdatedAt);
    }

    [Fact]
    public void Create_RejectsNonUtcDates()
    {
        var localTime = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            Partner.Create("Ana Silva", "123456789", PartnerType.Customer, null, 0m, localTime));
    }
}
