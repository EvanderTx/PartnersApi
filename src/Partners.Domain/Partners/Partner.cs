using System.Net.Mail;
using Partners.Domain.Common;

namespace Partners.Domain.Partners;

public sealed class Partner
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const decimal CreditLimitMin = 0m;
    public const decimal CreditLimitMax = 1_000_000m;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string TaxNumber { get; private set; } = string.Empty;
    public PartnerType Type { get; private set; }
    public string? Email { get; private set; }
    public decimal CreditLimit { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    // Required by EF Core to materialize entities from the database.
    private Partner() { }

    public static Partner Create(
        string? name, string? taxNumber, PartnerType type,
        string? email, decimal creditLimit, DateTime utcNow)
    {
        var values = Validate(name, taxNumber, type, email, creditLimit);

        return new Partner
        {
            Id = Guid.NewGuid(),
            Name = values.Name,
            TaxNumber = values.TaxNumber,
            Type = type,
            Email = values.Email,
            CreditLimit = creditLimit,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Update(
        string? name, string? taxNumber, PartnerType type,
        string? email, decimal creditLimit, bool isActive, DateTime utcNow)
    {
        // Validate everything first so an invalid update never changes the entity partially.
        var values = Validate(name, taxNumber, type, email, creditLimit);

        Name = values.Name;
        TaxNumber = values.TaxNumber;
        Type = type;
        Email = values.Email;
        CreditLimit = creditLimit;
        IsActive = isActive;
        UpdatedAt = utcNow;
    }

    private static ValidatedValues Validate(
        string? name, string? taxNumber, PartnerType type, string? email, decimal creditLimit)
    {
        var errors = new Dictionary<string, string[]>();

        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length is < NameMinLength or > NameMaxLength)
            errors[nameof(Name)] = [$"Name is required and must have between {NameMinLength} and {NameMaxLength} characters."];

        var normalizedTaxNumber = TaxNumberRules.Normalize(taxNumber);
        if (!TaxNumberRules.IsValid(normalizedTaxNumber))
            errors[nameof(TaxNumber)] = [$"TaxNumber is required and must contain exactly {TaxNumberRules.Length} digits (spaces, dots and hyphens are ignored)."];

        if (!Enum.IsDefined(type))
            errors[nameof(Type)] = ["Type must be CUSTOMER, SUPPLIER or BOTH."];

        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        if (normalizedEmail is not null && !IsValidEmail(normalizedEmail))
            errors[nameof(Email)] = ["Email has an invalid format."];

        if (creditLimit is < CreditLimitMin or > CreditLimitMax)
            errors[nameof(CreditLimit)] = [$"CreditLimit must be between {CreditLimitMin} and {CreditLimitMax:0}."];
        else if (decimal.Round(creditLimit, 2) != creditLimit)
            errors[nameof(CreditLimit)] = ["CreditLimit must have at most 2 decimal places."];

        if (errors.Count > 0)
            throw new DomainValidationException(errors);

        return new ValidatedValues(trimmedName, normalizedTaxNumber, normalizedEmail);
    }

    private static bool IsValidEmail(string email) =>
        email.Length <= EmailMaxLength
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email;

    private readonly record struct ValidatedValues(string Name, string TaxNumber, string? Email);
}