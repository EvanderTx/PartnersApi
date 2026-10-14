using Partners.Domain.Partners;

namespace Partners.Application.Partners.Dtos;

public sealed record CreatePartnerRequest
{
    public required string Name { get; init; }
    public required string TaxNumber { get; init; }
    public required PartnerType Type { get; init; }
    public string? Email { get; init; }
    public required decimal CreditLimit { get; init; }
}