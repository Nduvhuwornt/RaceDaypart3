using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RequireLogin]
public class EventsController : Controller
{
    private readonly IApiClient _api;

    public EventsController(IApiClient api) => _api = api;

    /// <summary>Full event information and categories, with an "Enter event" form for Participants.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetEventAsync(id);
        var session = HttpContext.Session;

        var model = new EventDetailsViewModel
        {
            Event = ev,
            IsParticipant = session.IsParticipant(),
            IsOwner = session.IsOrganiser() && ev.OrganiserId == session.GetUserId()
        };

        if (model.IsParticipant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            model.MyEnrolment = mine.FirstOrDefault(e => e.EventId == id);
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireLogin(Roles = SessionUser.ParticipantRole)]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category before entering.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        try
        {
            await _api.EnrolAsync(eventId, categoryId);
            TempData["Success"] = "You're in! Your entry has been recorded.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}
