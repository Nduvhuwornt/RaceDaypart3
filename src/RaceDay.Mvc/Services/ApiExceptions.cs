namespace RaceDay.Mvc.Services;

/// <summary>The API answered with an error. The message is safe to show to users.</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyList<string> Errors { get; }

    public ApiException(int statusCode, string message, IReadOnlyList<string>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new List<string>();
    }
}

/// <summary>The API no longer recognises the session (expired or logged out).</summary>
public class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("The API session has expired.")
    {
    }
}
