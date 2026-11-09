using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly IApiClient _api;

    public AccountController(IApiClient api) => _api = api;

    // ---------- Login ----------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (HttpContext.Session.IsLoggedIn())
        {
            return RedirectToRoleHome();
        }

        if (expired)
        {
            ViewData["Notice"] = "Your session has expired. Please log in again.";
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _api.LoginAsync(model.Email, model.Password);
            HttpContext.Session.SignIn(result.User, result.SessionCookie);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToRoleHome();
        }
        catch (ApiException ex)
        {
            ModelState.AddApiError(ex);
            return View(model);
        }
    }

    // ---------- Register ----------
    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsLoggedIn())
        {
            return RedirectToRoleHome();
        }

        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == SessionUser.OrganiserRole && string.IsNullOrWhiteSpace(model.OrganiserCode))
        {
            ModelState.AddModelError(nameof(model.OrganiserCode), "Organisers need the organiser registration code.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _api.RegisterAsync(new RegisterRequest
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                OrganiserCode = model.Role == SessionUser.OrganiserRole ? model.OrganiserCode : null
            });

            var login = await _api.LoginAsync(model.Email, model.Password);
            HttpContext.Session.SignIn(login.User, login.SessionCookie);
            TempData["Success"] = $"Welcome to RaceDay, {login.User.FullName}!";
            return RedirectToRoleHome();
        }
        catch (ApiException ex)
        {
            ModelState.AddApiError(ex);
            return View(model);
        }
    }

    // ---------- Logout ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (HttpContext.Session.IsLoggedIn())
        {
            try
            {
                await _api.LogoutAsync();
            }
            catch (ApiException)
            {
                // The local session is cleared below regardless.
            }
            catch (ApiUnauthorizedException)
            {
                // Already logged out on the API side.
            }
        }

        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    // ---------- Profile ----------
    [RequireLogin]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var profile = await _api.GetProfileAsync();
        return View(ToViewModel(profile));
    }

    [RequireLogin]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillReadOnlyFieldsAsync(model);
            return View(model);
        }

        try
        {
            var current = await _api.GetProfileAsync();
            var updated = await _api.UpdateProfileAsync(new UpdateProfileRequest
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                ProfilePictureUrl = current.ProfilePictureUrl
            });

            HttpContext.Session.SetFullName(updated.FullName);
            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Profile));
        }
        catch (ApiException ex)
        {
            ModelState.AddApiError(ex);
            await FillReadOnlyFieldsAsync(model);
            return View(model);
        }
    }

    [RequireLogin]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image to upload first.";
            return RedirectToAction(nameof(Profile));
        }

        try
        {
            await _api.UploadProfilePictureAsync(picture);
            TempData["Success"] = "Your profile picture has been updated.";
        }
        catch (ApiException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Profile));
    }

    // ---------- Helpers ----------
    private IActionResult RedirectToRoleHome() =>
        HttpContext.Session.IsOrganiser()
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");

    private static ProfileViewModel ToViewModel(UserProfile profile) => new()
    {
        FullName = profile.FullName,
        Email = profile.Email,
        PhoneNumber = profile.PhoneNumber,
        ProfilePictureUrl = profile.ProfilePictureUrl,
        Role = profile.Role
    };

    private async Task FillReadOnlyFieldsAsync(ProfileViewModel model)
    {
        try
        {
            var profile = await _api.GetProfileAsync();
            model.Email = profile.Email;
            model.ProfilePictureUrl = profile.ProfilePictureUrl;
            model.Role = profile.Role;
        }
        catch (ApiException)
        {
            // The form is still usable without these display-only values.
        }
    }
}
