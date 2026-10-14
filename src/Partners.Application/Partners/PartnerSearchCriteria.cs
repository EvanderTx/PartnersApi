using Partners.Application.Common.Exceptions;
using Partners.Application.Partners.Dtos;
using Partners.Domain.Partners;

namespace Partners.Application.Partners;

public sealed record PartnerSearchCriteria(
    string? NameContains,
    string? TaxNumberContains,
    IReadOnlyCollection<PartnerType>? Types,
    bool IncludeInactive)
{
    public static PartnerSearchCriteria From(PartnerListQuery query)
    {
        var search = query.Search?.Trim();
        var nameContains = string.IsNullOrEmpty(search) ? null : search;

        // Only search by TaxNumber when the normalized text is made of digits;
        // otherwise "Ana" or "---" would normalize to something that matches every TaxNumber.
        var normalizedTaxNumber = TaxNumberRules.Normalize(search);
        var taxNumberContains = normalizedTaxNumber.Length > 0 && normalizedTaxNumber.All(char.IsAsciiDigit)
            ? normalizedTaxNumber
            : null;

        return new PartnerSearchCriteria(nameContains, taxNumberContains, TypesMatching(query.Type), query.IncludeInactive);
    }

    private static PartnerType[]? TypesMatching(PartnerType? type) => type switch
    {
        null => null,
        PartnerType.Customer => [PartnerType.Customer, PartnerType.Both],
        PartnerType.Supplier => [PartnerType.Supplier, PartnerType.Both],
        PartnerType.Both => [PartnerType.Both],
        _ => throw new InvalidRequestException(nameof(PartnerListQuery.Type), "Type must be CUSTOMER, SUPPLIER or BOTH.")
    };
}
