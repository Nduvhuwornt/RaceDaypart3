using System.Globalization;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

public static class ViewHelpers
{
    public static string TypeIcon(string type) => type switch
    {
        "Run" => "bi-lightning-charge-fill",
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-flag-fill"
    };

    public static string TypeCss(string type) => "rd-type-" + type.ToLowerInvariant();

    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return $"{number}th";
        }

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    public static string Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Length <= max ? text : text[..max].TrimEnd() + "...";
    }

    public static string FormatDate(DateTime date) => date.ToString("ddd, d MMM yyyy 'at' HH:mm", CultureInfo.InvariantCulture);

    public static string FormatShortDate(DateTime date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    public static string FormatDistance(decimal km) => km.ToString("0.##", CultureInfo.InvariantCulture) + " km";

    public static string FormatTime(TimeSpan time) => time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "?";
        if (parts.Length == 1) return parts[0][..1].ToUpperInvariant();
        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    /// <summary>Short description of a category, e.g. "Ages 20-59 - 10 km".</summary>
    public static string CategoryDetail(Category category)
    {
        var parts = new List<string>();

        if (category.MinAge.HasValue && category.MaxAge.HasValue) parts.Add($"Ages {category.MinAge}-{category.MaxAge}");
        else if (category.MinAge.HasValue) parts.Add($"Ages {category.MinAge}+");
        else if (category.MaxAge.HasValue) parts.Add($"Up to age {category.MaxAge}");

        if (category.DistanceKm.HasValue) parts.Add(FormatDistance(category.DistanceKm.Value));

        return string.Join(" - ", parts);
    }
}
