using Partners.Domain.Partners;

namespace Partners.Application.Partners.Dtos;

public sealed record UpdatePartnerRequest
{
    public required string Name { get; init; }
    public required string TaxNumber { get; init; }
    public required PartnerType Type { get; init; }
    public required string? Email { get; init; }
    public required decimal CreditLimit { get; init; }
    public required bool IsActive { get; init; }
    public required string Version { get; init; }
}