using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[Route("api/users/me")]
[SessionAuthorize]
public class UsersController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;
    private readonly ILogger<UsersController> _logger;

    public UsersController(RaceDayDbContext db, IBlobStorageService blobs, ILogger<UsersController> logger)
    {
        _db = db;
        _blobs = blobs;
        _logger = logger;
    }

    /// <summary>Returns the profile of the logged-in user (Organiser or Participant).</summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">No active session.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.AsNoTracking().Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user is null)
        {
            HttpContext.Session.Clear();
            return ProblemResult(StatusCodes.Status401Unauthorized, "Unauthorized", "Your session is no longer valid. Please log in again.");
        }

        return Ok(new UserProfileDto(user.UserId, user.FullName, user.Email, user.PhoneNumber, user.ProfilePictureUrl, user.Role.RoleName));
    }

    /// <summary>Updates the logged-in user's own profile details.</summary>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">No active session.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return ProblemResult(StatusCodes.Status401Unauthorized, "Unauthorized", "Your session is no longer valid. Please log in again.");
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.ProfilePictureUrl = request.ProfilePictureUrl?.Trim();
        await _db.SaveChangesAsync();

        return Ok(new UserProfileDto(user.UserId, user.FullName, user.Email, user.PhoneNumber, user.ProfilePictureUrl, user.Role.RoleName));
    }

    /// <summary>Uploads a profile picture for the logged-in user (stored in Azure Blob Storage).</summary>
    /// <remarks>Send multipart/form-data with a single file field named "file" (JPG, PNG, WEBP or GIF, up to 5 MB).</remarks>
    /// <response code="200">Picture stored; returns its public URL.</response>
    /// <response code="400">Missing, oversized or invalid image.</response>
    /// <response code="401">No active session.</response>
    /// <response code="502">Azure Blob Storage rejected the upload.</response>
    /// <response code="503">Image storage is not configured.</response>
    [HttpPost("profile-picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return ProblemResult(StatusCodes.Status401Unauthorized, "Unauthorized", "Your session is no longer valid. Please log in again.");
        }

        return await StoreImageAsync(_blobs, _logger, file, $"profiles/{user.UserId}", async url =>
        {
            user.ProfilePictureUrl = url;
            await _db.SaveChangesAsync();
        });
    }
}
