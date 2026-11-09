using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

/// <summary>
/// Catches API failures that a controller did not handle itself, so users see a friendly page instead of a crash.
/// An expired API session sends the user back to the login page.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ApiUnauthorizedException:
                context.HttpContext.Session.Clear();
                context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
                context.ExceptionHandled = true;
                break;

            case ApiException apiException:
                context.Result = new RedirectToActionResult("Error", "Home", new { code = apiException.StatusCode });
                context.ExceptionHandled = true;
                break;
        }
    }
}
