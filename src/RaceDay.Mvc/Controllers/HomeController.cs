using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IApiClient _api;

    public HomeController(IApiClient api) => _api = api;

    /// <summary>Participant home page: all upcoming events with filters. Organisers go to their dashboard.</summary>
    [RequireLogin]
    public async Task<IActionResult> Index(string? type, string? search)
    {
        if (HttpContext.Session.IsOrganiser())
        {
            return RedirectToAction("Dashboard", "Organiser");
        }

        var events = await _api.GetEventsAsync(type, search, upcomingOnly: true);
        return View(new HomeIndexViewModel { Events = events, Type = type, Search = search });
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int code = 500)
    {
        var model = code switch
        {
            400 => new ErrorViewModel { Code = 400, Title = "That didn't work", Message = "Something in that request wasn't valid. Please go back and try again." },
            403 => new ErrorViewModel { Code = 403, Title = "Off the course", Message = "You don't have permission to do that." },
            404 => new ErrorViewModel { Code = 404, Title = "Lost the trail", Message = "We couldn't find what you were looking for. It may have been removed." },
            409 => new ErrorViewModel { Code = 409, Title = "Already on the start line", Message = "That change conflicts with something that already exists." },
            503 => new ErrorViewModel { Code = 503, Title = "Pit stop", Message = "The RaceDay service is currently unavailable. Please try again in a moment." },
            _ => new ErrorViewModel { Code = 500, Title = "Something tripped us up", Message = "An unexpected problem occurred. Please try again." }
        };

        Response.StatusCode = model.Code;
        return View(model);
    }
}
