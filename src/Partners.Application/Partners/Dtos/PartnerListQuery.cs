using Partners.Domain.Partners;

namespace Partners.Application.Partners.Dtos;

public sealed record PartnerListQuery
{
    public string? Search { get; init; }
    public PartnerType? Type { get; init; }
    public bool IncludeInactive { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}