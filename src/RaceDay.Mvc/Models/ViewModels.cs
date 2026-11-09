using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number."), StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Please choose a password."), StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password."), DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a role.")]
    public string Role { get; set; } = "Participant";

    [Display(Name = "Organiser code")]
    public string? OrganiserCode { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number."), StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class HomeIndexViewModel
{
    public List<EventSummary> Events { get; set; } = new();
    public string? Type { get; set; }
    public string? Search { get; set; }
}

public class EventDetailsViewModel
{
    public EventDetail Event { get; set; } = new();
    public Enrolment? MyEnrolment { get; set; }
    public bool IsParticipant { get; set; }
    public bool IsOwner { get; set; }
    public bool IsUpcoming => Event.EventDate >= DateTime.UtcNow;
}

public class DashboardViewModel
{
    public List<EventSummary> Events { get; set; } = new();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public List<EventSummary> Upcoming => Events.Where(e => e.EventDate >= DateTime.UtcNow).OrderBy(e => e.EventDate).ToList();
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(150, MinimumLength = 3)]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date and time.")]
    [Display(Name = "Date and time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public string? BannerImageUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }

    public EventRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description,
        EventDate = EventDate!.Value,
        Location = Location,
        DistanceKm = DistanceKm,
        EventType = EventType
    };
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name."), StringLength(50)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    public CategoryRequest ToRequest() => new() { Name = Name, MinAge = MinAge, MaxAge = MaxAge, DistanceKm = DistanceKm };
}

public class CategoriesViewModel
{
    public EventDetail Event { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EnrolmentsViewModel
{
    public EventDetail Event { get; set; } = new();
    public List<EventEnrolment> Enrolments { get; set; } = new();
}

public class ResultsViewModel
{
    public EventDetail Event { get; set; } = new();
    public List<EventEnrolment> Enrolments { get; set; } = new();
    public List<RaceResult> Results { get; set; } = new();
    public bool EventHasHappened => Event.EventDate <= DateTime.UtcNow;
}

public class ErrorViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
