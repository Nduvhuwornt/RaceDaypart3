using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

public interface IApiClient
{
    Task<LoginResult> LoginAsync(string email, string password);
    Task RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<UserProfile> GetProfileAsync();
    Task<UserProfile> UpdateProfileAsync(UpdateProfileRequest request);
    Task<string> UploadProfilePictureAsync(IFormFile file);

    Task<List<EventSummary>> GetEventsAsync(string? type = null, string? search = null, bool upcomingOnly = true, bool mine = false);
    Task<EventDetail> GetEventAsync(int id);
    Task<EventSummary> CreateEventAsync(EventRequest request);
    Task<EventSummary> UpdateEventAsync(int id, EventRequest request);
    Task DeleteEventAsync(int id);
    Task<string> UploadBannerAsync(int eventId, IFormFile file);

    Task<Category> CreateCategoryAsync(int eventId, CategoryRequest request);
    Task<Category> UpdateCategoryAsync(int id, CategoryRequest request);
    Task DeleteCategoryAsync(int id);

    Task<Enrolment> EnrolAsync(int eventId, int categoryId);
    Task<List<Enrolment>> GetMyEnrolmentsAsync();
    Task<List<EventEnrolment>> GetEventEnrolmentsAsync(int eventId);

    Task<RaceResult> CaptureResultAsync(int enrolmentId, ResultRequest request);
    Task<List<RaceResult>> GetMyResultsAsync();
    Task<List<RaceResult>> GetEventResultsAsync(int eventId);
}

/// <summary>
/// The only way the MVC app talks to the outside world: every read and write (including image uploads)
/// goes through the RaceDay API. There is no database or Azure access in this project.
/// </summary>
public class ApiClient : IApiClient
{
    private const string UnavailableMessage = "The RaceDay service is currently unavailable. Please try again in a moment.";
    private const string GenericMessage = "Something went wrong while talking to the RaceDay service. Please try again.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    // ---------- Auth ----------
    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login") { Content = JsonBody(new { email, password }) };
        using var response = await SendAsync(request, sessionExpiryOn401: false);
        var user = await ReadAsync<AuthResponse>(response);

        var cookie = ExtractSessionCookie(response);
        if (cookie is null)
        {
            throw new ApiException(502, "The server did not start a session. Please try again.");
        }

        return new LoginResult(user, cookie);
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/register") { Content = JsonBody(request) };
        using var response = await SendAsync(message, sessionExpiryOn401: false);
    }

    public async Task LogoutAsync()
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
        using var response = await SendAsync(message);
    }

    // ---------- Profile ----------
    public Task<UserProfile> GetProfileAsync() => GetAsync<UserProfile>("api/users/me");

    public Task<UserProfile> UpdateProfileAsync(UpdateProfileRequest request) =>
        SendJsonAsync<UserProfile>(HttpMethod.Put, "api/users/me", request);

    public Task<string> UploadProfilePictureAsync(IFormFile file) => UploadImageAsync("api/users/me/profile-picture", file);

    // ---------- Events ----------
    public Task<List<EventSummary>> GetEventsAsync(string? type = null, string? search = null, bool upcomingOnly = true, bool mine = false)
    {
        var query = new Dictionary<string, string?>
        {
            ["upcomingOnly"] = upcomingOnly ? "true" : "false",
            ["mine"] = mine ? "true" : "false"
        };
        if (!string.IsNullOrWhiteSpace(type)) query["type"] = type;
        if (!string.IsNullOrWhiteSpace(search)) query["search"] = search;

        return GetAsync<List<EventSummary>>(QueryHelpers.AddQueryString("api/events", query));
    }

    public Task<EventDetail> GetEventAsync(int id) => GetAsync<EventDetail>($"api/events/{id}");

    public Task<EventSummary> CreateEventAsync(EventRequest request) =>
        SendJsonAsync<EventSummary>(HttpMethod.Post, "api/events", request);

    public Task<EventSummary> UpdateEventAsync(int id, EventRequest request) =>
        SendJsonAsync<EventSummary>(HttpMethod.Put, $"api/events/{id}", request);

    public Task DeleteEventAsync(int id) => DeleteAsync($"api/events/{id}");

    public Task<string> UploadBannerAsync(int eventId, IFormFile file) => UploadImageAsync($"api/events/{eventId}/banner", file);

    // ---------- Categories ----------
    public Task<Category> CreateCategoryAsync(int eventId, CategoryRequest request) =>
        SendJsonAsync<Category>(HttpMethod.Post, $"api/events/{eventId}/categories", request);

    public Task<Category> UpdateCategoryAsync(int id, CategoryRequest request) =>
        SendJsonAsync<Category>(HttpMethod.Put, $"api/categories/{id}", request);

    public Task DeleteCategoryAsync(int id) => DeleteAsync($"api/categories/{id}");

    // ---------- Enrolments ----------
    public Task<Enrolment> EnrolAsync(int eventId, int categoryId) =>
        SendJsonAsync<Enrolment>(HttpMethod.Post, $"api/events/{eventId}/enrolments", new { categoryId });

    public Task<List<Enrolment>> GetMyEnrolmentsAsync() => GetAsync<List<Enrolment>>("api/users/me/enrolments");

    public Task<List<EventEnrolment>> GetEventEnrolmentsAsync(int eventId) =>
        GetAsync<List<EventEnrolment>>($"api/events/{eventId}/enrolments");

    // ---------- Results ----------
    public Task<RaceResult> CaptureResultAsync(int enrolmentId, ResultRequest request) =>
        SendJsonAsync<RaceResult>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", request);

    public Task<List<RaceResult>> GetMyResultsAsync() => GetAsync<List<RaceResult>>("api/users/me/results");

    public Task<List<RaceResult>> GetEventResultsAsync(int eventId) => GetAsync<List<RaceResult>>($"api/events/{eventId}/results");

    // ---------- Plumbing ----------
    private static HttpContent JsonBody(object body) => JsonContent.Create(body, options: Json);

    private async Task<T> GetAsync<T>(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await SendAsync(request);
        return await ReadAsync<T>(response);
    }

    private async Task<T> SendJsonAsync<T>(HttpMethod method, string url, object body)
    {
        using var request = new HttpRequestMessage(method, url) { Content = JsonBody(body) };
        using var response = await SendAsync(request);
        return await ReadAsync<T>(response);
    }

    private async Task DeleteAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, url);
        using var response = await SendAsync(request);
    }

    private async Task<string> UploadImageAsync(string url, IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType)
            ? contentType
            : new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", file.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        using var response = await SendAsync(request);
        var result = await ReadAsync<ImageUploadResult>(response);
        return result.Url;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool sessionExpiryOn401 = true)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(503, UnavailableMessage);
        }
        catch (TaskCanceledException)
        {
            throw new ApiException(503, UnavailableMessage);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        try
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && sessionExpiryOn401)
            {
                throw new ApiUnauthorizedException();
            }

            throw await BuildExceptionAsync(response);
        }
        finally
        {
            response.Dispose();
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(Json);
            return value ?? throw new ApiException(502, GenericMessage);
        }
        catch (JsonException)
        {
            throw new ApiException(502, GenericMessage);
        }
    }

    /// <summary>Turns an API error response (problem details) into a friendly exception; server faults are never shown raw.</summary>
    private static async Task<ApiException> BuildExceptionAsync(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        if (status >= 500 && status != 502 && status != 503)
        {
            return new ApiException(status, GenericMessage);
        }

        string? detail = null;
        var errors = new List<string>();

        try
        {
            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errorsElement.EnumerateObject())
                    {
                        if (field.Value.ValueKind != JsonValueKind.Array) continue;
                        foreach (var message in field.Value.EnumerateArray())
                        {
                            var value = message.GetString();
                            if (!string.IsNullOrWhiteSpace(value)) errors.Add(value);
                        }
                    }
                }

                if (root.TryGetProperty("detail", out var detailElement) && detailElement.ValueKind == JsonValueKind.String)
                {
                    detail = detailElement.GetString();
                }
                else if (root.TryGetProperty("title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String)
                {
                    detail = titleElement.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON: fall through to the generic message below.
        }

        var fallback = status == 503 ? UnavailableMessage : GenericMessage;
        var message2 = errors.Count > 0 ? string.Join(" ", errors) : detail ?? fallback;
        return new ApiException(status, message2, errors);
    }

    /// <summary>Finds the API's session cookie in the Set-Cookie headers and returns it as "name=value".</summary>
    private static string? ExtractSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var header in values)
        {
            if (header.StartsWith(".RaceDay.Session=", StringComparison.Ordinal))
            {
                var end = header.IndexOf(';');
                return end > 0 ? header[..end] : header;
            }
        }

        return null;
    }
}
