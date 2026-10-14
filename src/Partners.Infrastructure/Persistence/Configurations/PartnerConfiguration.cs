using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Partners.Domain.Partners;

namespace Partners.Infrastructure.Persistence.Configurations;

public sealed class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("Partners", table =>
        {
            table.HasCheckConstraint("CK_Partners_CreditLimit", "[CreditLimit] >= 0 AND [CreditLimit] <= 1000000");
            table.HasCheckConstraint("CK_Partners_Type", "[Type] IN ('Customer', 'Supplier', 'Both')");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasMaxLength(Partner.NameMaxLength)
            .IsRequired();

        builder.Property(p => p.TaxNumber)
            .HasMaxLength(TaxNumberRules.Length)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasMaxLength(Partner.EmailMaxLength);

        builder.Property(p => p.CreditLimit)
            .HasPrecision(18, 2);

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasIndex(p => p.TaxNumber)
            .IsUnique()
            .HasDatabaseName("UX_Partners_TaxNumber");

        builder.HasIndex(p => new { p.Name, p.Id })
            .HasDatabaseName("IX_Partners_Name_Id");

        builder.HasData(SeedData());
    }

    private static object[] SeedData()
    {
        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return
        [
            new
            {
                Id = Guid.Parse("8f1c2a10-0000-4000-8000-000000000001"),
                Name = "Carlos Mendes", TaxNumber = "500000001", Type = PartnerType.Customer,
                Email = (string?)"carlos.mendes@example.com", CreditLimit = 5000.00m,
                IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt
            },
            new
            {
                Id = Guid.Parse("8f1c2a10-0000-4000-8000-000000000002"),
                Name = "Distribuidora Kwanza Lda", TaxNumber = "500000002", Type = PartnerType.Supplier,
                Email = (string?)"geral@kwanza.example.com", CreditLimit = 0.00m,
                IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt
            },
            new
            {
                Id = Guid.Parse("8f1c2a10-0000-4000-8000-000000000003"),
                Name = "Beta Comercial SA", TaxNumber = "500000003", Type = PartnerType.Both,
                Email = (string?)null, CreditLimit = 250000.50m,
                IsActive = true, CreatedAt = seededAt, UpdatedAt = seededAt
            },
            new
            {
                Id = Guid.Parse("8f1c2a10-0000-4000-8000-000000000004"),
                Name = "Maria Santos", TaxNumber = "500000004", Type = PartnerType.Customer,
                Email = (string?)"maria.santos@example.com", CreditLimit = 1200.75m,
                IsActive = false, CreatedAt = seededAt, UpdatedAt = seededAt
            }
        ];
    }
}