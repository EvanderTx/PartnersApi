namespace Partners.Application.Common.Exceptions;

public sealed class InvalidRequestException(string field, string message)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } =
        new Dictionary<string, string[]> { [field] = [message] };
}