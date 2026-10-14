using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Partners.Infrastructure.Persistence.Converters;

// SQL Server datetime2 has no time zone; values read back are marked as UTC.
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toDatabase => toDatabase,
    fromDatabase => DateTime.SpecifyKind(fromDatabase, DateTimeKind.Utc));