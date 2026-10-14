using Partners.Domain.Partners;

namespace Partners.Application.Partners.Dtos;

public sealed record PartnerResponse(
    Guid Id,
    string Name,
    string TaxNumber,
    PartnerType Type,
    string? Email,
    decimal CreditLimit,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Version)
{
    public static PartnerResponse FromEntity(Partner partner) => new(
        partner.Id,
        partner.Name,
        partner.TaxNumber,
        partner.Type,
        partner.Email,
        partner.CreditLimit,
        partner.IsActive,
        partner.CreatedAt,
        partner.UpdatedAt,
        VersionToken.Encode(partner.RowVersion));
}