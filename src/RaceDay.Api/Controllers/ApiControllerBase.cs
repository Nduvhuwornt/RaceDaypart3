using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>ID of the logged-in user, read from the server-side session.</summary>
    protected int CurrentUserId =>
        HttpContext.Session.GetUserId() ?? throw new InvalidOperationException("No user in session.");

    protected string CurrentRole => HttpContext.Session.GetRole() ?? string.Empty;

    protected ObjectResult ProblemResult(int status, string title, string detail) =>
        StatusCode(status, new ProblemDetails { Status = status, Title = title, Detail = detail });

    protected ObjectResult NotFoundProblem(string detail) => ProblemResult(StatusCodes.Status404NotFound, "Not Found", detail);
    protected ObjectResult ForbiddenProblem(string detail) => ProblemResult(StatusCodes.Status403Forbidden, "Forbidden", detail);
    protected ObjectResult ConflictProblem(string detail) => ProblemResult(StatusCodes.Status409Conflict, "Conflict", detail);
    protected ObjectResult BadRequestProblem(string detail) => ProblemResult(StatusCodes.Status400BadRequest, "Bad Request", detail);

    /// <summary>Validates an uploaded image, stores it in Azure Blob Storage and lets the caller save the resulting URL.</summary>
    protected async Task<IActionResult> StoreImageAsync(
        IBlobStorageService blobs, ILogger logger, IFormFile? file, string folder, Func<string, Task> saveUrlAsync)
    {
        var (error, extension, contentType) = await ImageValidator.ValidateAsync(file);
        if (error is not null)
        {
            return BadRequestProblem(error);
        }

        try
        {
            await using var stream = file!.OpenReadStream();
            var url = await blobs.UploadImageAsync(stream, $"{folder}/{Guid.NewGuid():N}{extension}", contentType);
            await saveUrlAsync(url);
            return Ok(new ImageUploadResponse(url));
        }
        catch (BlobStorageNotConfiguredException)
        {
            return ProblemResult(StatusCodes.Status503ServiceUnavailable, "Service Unavailable", "Image storage is not configured on the server.");
        }
        catch (Azure.RequestFailedException ex)
        {
            logger.LogError(ex, "Azure Blob Storage upload failed ({ErrorCode}).", ex.ErrorCode);
            var hint = ex.Status is 401 or 403
                ? "Azure rejected the credentials. Check the storage account connection string (AccountName and AccountKey)."
                : "Azure Blob Storage returned an error. Please try again.";
            return ProblemResult(StatusCodes.Status502BadGateway, "Bad Gateway", $"{hint} (Azure error: {ex.ErrorCode ?? ex.Status.ToString()})");
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
            logger.LogError(ex, "Azure Blob Storage is configured incorrectly.");
            return ProblemResult(StatusCodes.Status503ServiceUnavailable, "Service Unavailable",
                "The Azure storage connection string is not valid. Check AzureBlobStorage:ConnectionString in your user secrets.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not reach Azure Blob Storage.");
            return ProblemResult(StatusCodes.Status503ServiceUnavailable, "Service Unavailable",
                "Azure Blob Storage could not be reached. Check the AccountName in the connection string and your internet connection.");
        }
    }
}
