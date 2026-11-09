using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RequireLogin(Roles = SessionUser.ParticipantRole)]
public class ParticipantController : Controller
{
    private readonly IApiClient _api;

    public ParticipantController(IApiClient api) => _api = api;

    /// <summary>Every event the Participant has entered, with colour-coded status.</summary>
    public async Task<IActionResult> MyEvents()
    {
        var enrolments = await _api.GetMyEnrolmentsAsync();
        return View(enrolments);
    }

    /// <summary>Personal race history: event, date, category, finish time and position.</summary>
    public async Task<IActionResult> MyResults()
    {
        var results = await _api.GetMyResultsAsync();
        return View(results);
    }
}
