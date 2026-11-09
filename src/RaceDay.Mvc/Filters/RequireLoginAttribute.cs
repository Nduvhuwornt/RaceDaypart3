using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// Session-based page protection. Not logged in: redirect to the login page.
/// Logged in with the wrong role: redirect to the "access denied" page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireLoginAttribute : Attribute, IAuthorizationFilter
{
    public string? Roles { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsLoggedIn())
        {
            var request = context.HttpContext.Request;
            object? routeValues = HttpMethods.IsGet(request.Method)
                ? new { returnUrl = request.Path + request.QueryString }
                : null;
            context.Result = new RedirectToActionResult("Login", "Account", routeValues);
            return;
        }

        if (!string.IsNullOrWhiteSpace(Roles))
        {
            var allowed = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var role = session.GetRole();
            if (role is null || !allowed.Contains(role))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            }
        }
    }
}
