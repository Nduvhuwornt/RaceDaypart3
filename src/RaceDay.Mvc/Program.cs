using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.AddHttpContextAccessor();

// MVC's own server-side session: holds the user's ID, name, role and the API's session cookie
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Mvc.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});

// Typed HttpClient for the RaceDay API. Cookies are attached per user by ApiSessionHandler,
// so the handler's own shared cookie container must be switched off.
builder.Services.AddTransient<ApiSessionHandler>();
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
    {
        var baseUrl = builder.Configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseCookies = false })
    .AddHttpMessageHandler<ApiSessionHandler>();

var app = builder.Build();

// Fixed culture so decimals ("10.5") and dates bind the same on every machine (SA machines default to a comma).
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures("en-US")
    .AddSupportedUICultures("en-US"));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error?code=500");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
