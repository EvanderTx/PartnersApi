using Partners.Domain.Partners;

namespace Partners.Application.Partners;

public interface IPartnerRepository
{
    Task<Partner?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> CountAsync(PartnerSearchCriteria criteria, CancellationToken cancellationToken);
    Task<IReadOnlyList<Partner>> ListAsync(PartnerSearchCriteria criteria, int skip, int take, CancellationToken cancellationToken);
    void Add(Partner partner);
    void SetExpectedVersion(Partner partner, byte[] expectedVersion);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}