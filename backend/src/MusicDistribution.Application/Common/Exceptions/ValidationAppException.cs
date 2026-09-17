namespace MusicDistribution.Application.Common.Exceptions;

/// <summary>Thrown for business-rule validation failures (e.g. duplicate ISRC). Mapped to HTTP 400 by the API layer.</summary>
public class ValidationAppException : Exception
{
    public ValidationAppException(string message) : base(message) { }
}
