using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Services;

/// <summary>
/// Attaches the logged-in user's API session cookie (kept in the MVC session) to every outgoing API request,
/// so the API sees the right user and role.
/// </summary>
public class ApiSessionHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;

    public ApiSessionHandler(IHttpContextAccessor accessor) => _accessor = accessor;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cookie = _accessor.HttpContext?.Session.GetString(SessionUser.ApiCookieKey);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
