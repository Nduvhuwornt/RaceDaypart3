using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RequireLogin(Roles = SessionUser.OrganiserRole)]
public class OrganiserController : Controller
{
    private readonly IApiClient _api;

    public OrganiserController(IApiClient api) => _api = api;

    // ---------- Dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        var events = await _api.GetEventsAsync(upcomingOnly: false, mine: true);
        return View(new DashboardViewModel { Events = events });
    }

    // ---------- Events ----------
    public async Task<IActionResult> Events()
    {
        var events = await _api.GetEventsAsync(upcomingOnly: false, mine: true);
        return View(events.OrderByDescending(e => e.EventDate).ToList());
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        ViewData["Title"] = "Create event";
        return View("EventForm", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(7) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        ViewData["Title"] = "Create event";
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        try
        {
            var created = await _api.CreateEventAsync(model.ToRequest());
            var bannerWarning = await TryUploadBannerAsync(created.EventId, model.BannerFile);

            TempData["Success"] = "Event created. Add categories so participants can enter.";
            if (bannerWarning is not null)
            {
                TempData["Error"] = bannerWarning;
            }

            return RedirectToAction(nameof(Categories), new { eventId = created.EventId });
        }
        catch (ApiException ex)
        {
            ModelState.AddApiError(ex);
            return View("EventForm", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        ViewData["Title"] = "Edit event";
        var ev = await _api.GetEventAsync(id);
        return View("EventForm", new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            BannerImageUrl = ev.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormViewModel model)
    {
        ViewData["Title"] = "Edit event";
        var id = model.EventId;
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        try
        {
            await _api.UpdateEventAsync(id, model.ToRequest());
            var bannerWarning = await TryUploadBannerAsync(id, model.BannerFile);

            TempData["Success"] = "Event updated.";
            if (bannerWarning is not null)
            {
                TempData["Error"] = bannerWarning;
            }

            return RedirectToAction(nameof(Events));
        }
        catch (ApiException ex)
        {
            ModelState.AddApiError(ex);
            return View("EventForm", model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        try
        {
            await _api.DeleteEventAsync(id);
            TempData["Success"] = "Event deleted.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------
    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        return View(new CategoriesViewModel { Event = ev, NewCategory = new CategoryFormViewModel { EventId = eventId } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int eventId, CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return await RenderCategoriesAsync(eventId, form);
        }

        try
        {
            await _api.CreateCategoryAsync(eventId, form.ToRequest());
            TempData["Success"] = $"Category '{form.Name}' added.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }
        catch (ApiException ex)
        {
            ModelState.AddApiError(ex);
            return await RenderCategoriesAsync(eventId, form);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id, int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        var category = ev.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (category is null)
        {
            return RedirectToAction("Error", "Home", new { code = 404 });
        }

        ViewData["EventName"] = ev.Name;
        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            Name = category.Name,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, int eventId, CategoryFormViewModel form)
    {
        form.CategoryId = id;
        form.EventId = eventId;
        ViewData["EventName"] = (await _api.GetEventAsync(eventId)).Name;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await _api.UpdateCategoryAsync(id, form.ToRequest());
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }
        catch (ApiException ex)
        {
            ModelState.AddApiError(ex);
            return View(form);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        try
        {
            await _api.DeleteCategoryAsync(id);
            TempData["Success"] = "Category deleted.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        return View(new EnrolmentsViewModel { Event = ev, Enrolments = enrolments });
    }

    // ---------- Results ----------
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        var results = await _api.GetEventResultsAsync(eventId);
        return View(new ResultsViewModel { Event = ev, Enrolments = enrolments, Results = results });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(int enrolmentId, int eventId, string? finishTime, int finishPosition)
    {
        var formats = new[] { @"h\:mm\:ss", @"hh\:mm\:ss" };
        if (string.IsNullOrWhiteSpace(finishTime)
            || !TimeSpan.TryParseExact(finishTime.Trim(), formats, CultureInfo.InvariantCulture, out var time))
        {
            TempData["Error"] = "Enter the finish time as hh:mm:ss, for example 00:52:14.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        if (finishPosition < 1)
        {
            TempData["Error"] = "The finishing position must be 1 or higher.";
            return RedirectToAction(nameof(Results), new { eventId });
        }

        try
        {
            await _api.CaptureResultAsync(enrolmentId, new ResultRequest { FinishTime = time, FinishPosition = finishPosition });
            TempData["Success"] = "Result saved and published.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Results), new { eventId });
    }

    // ---------- Helpers ----------
    private async Task<IActionResult> RenderCategoriesAsync(int eventId, CategoryFormViewModel form)
    {
        var ev = await _api.GetEventAsync(eventId);
        form.EventId = eventId;
        return View("Categories", new CategoriesViewModel { Event = ev, NewCategory = form });
    }

    /// <summary>Uploads a banner through the API. Returns a warning message if it failed, otherwise null.</summary>
    private async Task<string?> TryUploadBannerAsync(int eventId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        try
        {
            await _api.UploadBannerAsync(eventId, file);
            return null;
        }
        catch (ApiException ex)
        {
            return $"The event was saved, but the banner could not be uploaded: {ex.Message}";
        }
    }
}
