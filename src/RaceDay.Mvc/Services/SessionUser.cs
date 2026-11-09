using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

/// <summary>Reads and writes the logged-in user's details held in the MVC session.</summary>
public static class SessionUser
{
    public const string OrganiserRole = "Organiser";
    public const string ParticipantRole = "Participant";

    private const string UserIdKey = "UserId";
    private const string RoleKey = "Role";
    private const string NameKey = "FullName";
    public const string ApiCookieKey = "ApiCookie";

    public static void SignIn(this ISession session, AuthResponse user, string apiCookie)
    {
        session.Clear();
        session.SetInt32(UserIdKey, user.UserId);
        session.SetString(RoleKey, user.Role);
        session.SetString(NameKey, user.FullName);
        session.SetString(ApiCookieKey, apiCookie);
    }

    public static void SetFullName(this ISession session, string fullName) => session.SetString(NameKey, fullName);

    public static bool IsLoggedIn(this ISession session) =>
        session.GetInt32(UserIdKey).HasValue && !string.IsNullOrEmpty(session.GetString(ApiCookieKey));

    public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);
    public static string? GetRole(this ISession session) => session.GetString(RoleKey);
    public static string GetFullName(this ISession session) => session.GetString(NameKey) ?? string.Empty;
    public static bool IsOrganiser(this ISession session) => session.GetRole() == OrganiserRole;
    public static bool IsParticipant(this ISession session) => session.GetRole() == ParticipantRole;
}
