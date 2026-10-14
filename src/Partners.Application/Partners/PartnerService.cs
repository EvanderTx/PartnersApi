using Partners.Application.Common;
using Partners.Application.Common.Exceptions;
using Partners.Application.Partners.Dtos;
using Partners.Domain.Partners;

namespace Partners.Application.Partners;

public sealed class PartnerService(IPartnerRepository repository, TimeProvider timeProvider)
{
    public const int MaxPageSize = 100;

    public async Task<PartnerResponse> CreateAsync(CreatePartnerRequest request, CancellationToken cancellationToken)
    {
        var partner = Partner.Create(
            request.Name, request.TaxNumber, request.Type, request.Email, request.CreditLimit, UtcNow());

        repository.Add(partner);
        await repository.SaveChangesAsync(cancellationToken);

        return PartnerResponse.FromEntity(partner);
    }

    public async Task<PartnerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var partner = await repository.GetByIdAsync(id, cancellationToken) ?? throw PartnerNotFound(id);
        return PartnerResponse.FromEntity(partner);
    }

    public async Task<PagedResult<PartnerResponse>> ListAsync(PartnerListQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            throw new InvalidRequestException(nameof(query.Page), "Page must be greater than or equal to 1.");
        if (query.PageSize is < 1 or > MaxPageSize)
            throw new InvalidRequestException(nameof(query.PageSize), $"PageSize must be between 1 and {MaxPageSize}.");

        var criteria = PartnerSearchCriteria.From(query);
        var totalCount = await repository.CountAsync(criteria, cancellationToken);

        // long avoids int overflow for very large Page values.
        var skip = (long)(query.Page - 1) * query.PageSize;
        IReadOnlyList<Partner> partners = skip >= totalCount
            ? []
            : await repository.ListAsync(criteria, (int)skip, query.PageSize, cancellationToken);

        return new PagedResult<PartnerResponse>(
            partners.Select(PartnerResponse.FromEntity).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<PartnerResponse> UpdateAsync(Guid id, UpdatePartnerRequest request, CancellationToken cancellationToken)
    {
        if (!VersionToken.TryDecode(request.Version, out var expectedVersion))
            throw new InvalidRequestException(nameof(request.Version), "Version is required and must be the value returned by the API.");

        var partner = await repository.GetByIdAsync(id, cancellationToken) ?? throw PartnerNotFound(id);

        partner.Update(
            request.Name, request.TaxNumber, request.Type, request.Email,
            request.CreditLimit, request.IsActive, UtcNow());

        // The UPDATE only succeeds if the row still has the version the client read.
        repository.SetExpectedVersion(partner, expectedVersion);
        await repository.SaveChangesAsync(cancellationToken);

        return PartnerResponse.FromEntity(partner);
    }

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    private static NotFoundException PartnerNotFound(Guid id) => new($"Partner '{id}' was not found.");
}
