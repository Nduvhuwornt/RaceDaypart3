using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RaceDay.Tests;

public class ImageUploadTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ImageUploadTests(RaceDayApiFactory factory) => _factory = factory;

    private static MultipartFormDataContent PngForm(string fileName = "banner.png")
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 1, 2, 3, 4 };
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent();
        form.Add(file, "file", fileName);
        return form;
    }

    private static MultipartFormDataContent TextForm(string fileName)
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("this is not an image"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent();
        form.Add(file, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Organiser_CanUploadBanner_AndEventStoresTheUrl()
    {
        var (organiser, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await ApiHelpers.CreateEventAsync(organiser);

        var upload = await organiser.PostAsync($"/api/events/{eventId}/banner", PngForm());

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var url = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString();
        Assert.StartsWith("https://fake.blob.core.windows.net/", url);

        var detail = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}");
        Assert.Equal(url, detail.GetProperty("bannerImageUrl").GetString());
    }

    [Fact]
    public async Task Participant_CannotUploadBanner_Returns403()
    {
        var (organiser, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var (participant, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await ApiHelpers.CreateEventAsync(organiser);

        var response = await participant.PostAsync($"/api/events/{eventId}/banner", PngForm());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUploadBannerForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var (other, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await ApiHelpers.CreateEventAsync(owner);

        var response = await other.PostAsync($"/api/events/{eventId}/banner", PngForm());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithoutLogin_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/events/1/banner", PngForm());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithWrongExtension_Returns400()
    {
        var (organiser, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await ApiHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", TextForm("notes.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WhenContentIsNotAnImage_Returns400()
    {
        var (organiser, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await ApiHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", TextForm("fake.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ForUnknownEvent_Returns404()
    {
        var (organiser, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsync("/api/events/999999/banner", PngForm());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanUploadProfilePicture_AndProfileShowsTheUrl()
    {
        var (participant, _) = await ApiHelpers.CreateLoggedInClientAsync(_factory, "Participant");

        var upload = await participant.PostAsync("/api/users/me/profile-picture", PngForm("me.png"));
        var profile = await participant.GetFromJsonAsync<JsonElement>("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        Assert.StartsWith("https://fake.blob.core.windows.net/", profile.GetProperty("profilePictureUrl").GetString());
    }
}
