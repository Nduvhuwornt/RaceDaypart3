using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RaceDay.Mvc.Services;

public static class ModelStateExtensions
{
    /// <summary>Shows an API error message (already user-friendly) in the form's validation summary.</summary>
    public static void AddApiError(this ModelStateDictionary modelState, ApiException exception)
    {
        if (exception.Errors.Count > 0)
        {
            foreach (var error in exception.Errors)
            {
                modelState.AddModelError(string.Empty, error);
            }
        }
        else
        {
            modelState.AddModelError(string.Empty, exception.Message);
        }
    }
}
