namespace DeviceManagement.Api.Domain.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string fieldName, string message)
        : this(new Dictionary<string, string[]>
        {
            [fieldName] = new[] { message }
        })
    {
    }

    public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
