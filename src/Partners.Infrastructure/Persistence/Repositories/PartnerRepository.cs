using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Partners.Application.Common.Exceptions;
using Partners.Application.Partners;
using Partners.Domain.Partners;

namespace Partners.Infrastructure.Persistence.Repositories;

internal sealed class PartnerRepository(AppDbContext dbContext) : IPartnerRepository
{
    private const string TaxNumberUniqueIndex = "UX_Partners_TaxNumber";
    private const string CaseInsensitiveCollation = "Latin1_General_CI_AS";

    public Task<Partner?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Partners.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<int> CountAsync(PartnerSearchCriteria criteria, CancellationToken cancellationToken) =>
        Filter(criteria).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Partner>> ListAsync(
        PartnerSearchCriteria criteria, int skip, int take, CancellationToken cancellationToken) =>
        await Filter(criteria)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(Partner partner) => dbContext.Partners.Add(partner);

    public void SetExpectedVersion(Partner partner, byte[] expectedVersion) =>
        dbContext.Entry(partner).Property(p => p.RowVersion).OriginalValue = expectedVersion;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The partner was changed by another request. Get the latest Version and try again.");
        }
        catch (DbUpdateException ex) when (IsTaxNumberUniqueViolation(ex))
        {
            throw new ConflictException("A partner with this TaxNumber already exists.");
        }
    }

    private IQueryable<Partner> Filter(PartnerSearchCriteria criteria)
    {
        var query = dbContext.Partners.AsNoTracking();

        if (!criteria.IncludeInactive)
            query = query.Where(p => p.IsActive);

        if (criteria.Types is { } types)
            query = query.Where(p => types.Contains(p.Type));

        if (criteria.NameContains is { } name)
        {
            query = criteria.TaxNumberContains is { } taxNumber
                ? query.Where(p => EF.Functions.Collate(p.Name, CaseInsensitiveCollation).Contains(name)
                                   || p.TaxNumber.Contains(taxNumber))
                : query.Where(p => EF.Functions.Collate(p.Name, CaseInsensitiveCollation).Contains(name));
        }

        return query;
    }

    private static bool IsTaxNumberUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 } sqlException
        && sqlException.Message.Contains(TaxNumberUniqueIndex);
}
