namespace Partners.Application.Partners;

// Converts the SQL Server rowversion (8 bytes) to and from the opaque Base64 Version exposed by the API.
public static class VersionToken
{
    private const int RowVersionLength = 8;

    public static string Encode(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static bool TryDecode(string? version, out byte[] rowVersion)
    {
        rowVersion = new byte[RowVersionLength];

        return !string.IsNullOrWhiteSpace(version)
            && Convert.TryFromBase64String(version, rowVersion, out var bytesWritten)
            && bytesWritten == RowVersionLength;
    }
}